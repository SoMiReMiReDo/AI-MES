using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Domain.Entities;
using MESagent.Application;
using MESagent.Infrastructure.EquipmentMaintenanceResources;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MESagent.Api.Tests;

public sealed partial class ExecutionTests
{
    private async Task<JsonElement> EquipmentRow(Sample sample) =>
        (await Get($"equipment/tools/{Id(sample.Tool)}")).GetProperty("equipment");

    private Guid OperationOf(Sample sample) => sample.Steps[0].GetProperty("operationId").GetGuid();

    private Task<JsonElement> ResourcePolicy(Sample sample, bool equipment = false, bool worker = false,
        string? layer = null, Guid[]? materials = null) => Ok("equipment/policies", new
        {
            operationId = OperationOf(sample),
            requireEquipmentQualification = equipment || worker,
            requireWorkerQualification = equipment || worker,
            requiredReticleLayer = layer,
            requiredMaterialIds = materials ?? []
        });

    private async Task<HttpResponseMessage> TryResourceStart(Sample sample)
    {
        var lot = (await Get($"execution/lots/{Id(sample.Lot)}")).GetProperty("lot");
        var equipment = await EquipmentRow(sample);
        return await Send($"execution/lots/{Id(lot)}/track-in", new
        {
            equipmentId = Id(equipment),
            equipmentEtag = E(equipment),
            recipeVersionId = Id(sample.Recipe)
        }, E(lot));
    }

    [Fact]
    public async Task Equipment_registry_versions_active_references_and_chambers_are_enforced()
    {
        var sample = await Setup();
        var textCode = "a" + Guid.NewGuid().ToString("N")[1..];
        var textKey = Guid.NewGuid().ToString();
        Assert.True((await Send("equipment/types", new { code = textCode, name = "文本编码" },
            key: textKey)).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send("equipment/types",
            new { code = textCode.ToUpperInvariant(), name = "文本编码" }, key: textKey)).StatusCode);
        var typeId = sample.Tool.GetProperty("equipmentTypeId").GetGuid();
        var type = (await Get($"equipment/types?id={typeId}")).GetProperty("items")[0];
        Assert.Equal(HttpStatusCode.BadRequest, (await Send($"equipment/types/{typeId}",
            new { name = "缺少启用标志" }, E(type), method: "PUT")).StatusCode);
        Assert.True((await Get($"equipment/types?id={typeId}")).GetProperty("items")[0]
            .GetProperty("isActive").GetBoolean());
        var edited = await Ok($"equipment/types/{typeId}", new { name = "停用类型", isActive = false }, E(type), "PUT");
        Assert.Equal(HttpStatusCode.PreconditionFailed,
            (await Send($"equipment/types/{typeId}", new { name = "旧版", isActive = true }, E(type),
                method: "PUT")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
        await Ok($"equipment/types/{typeId}", new { name = "已启用", isActive = true }, E(edited), "PUT");
        var chamber = await Ok("equipment/chambers", new
        {
            equipmentId = Id(sample.Tool),
            code = Guid.NewGuid().ToString("N"),
            name = "腔体台账"
        });
        await Ok($"equipment/chambers/{Id(chamber)}", new { name = "腔体停用", isActive = false }, E(chamber), "PUT");
        var otherType = await Ok("equipment/types", new { code = Guid.NewGuid().ToString("N"), name = "其他类型" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send("equipment/resources", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "不匹配工具",
            equipmentId = Id(sample.Tool),
            equipmentTypeId = Id(otherType)
        })).StatusCode);
        var run = await Start(sample);
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"equipment/tools/{Id(sample.Tool)}",
            new { name = "停用", isActive = false }, E(await EquipmentRow(sample)), method: "PUT")).StatusCode);
        await Ok($"execution/runs/{Id(run)}/abort", new { reason = "收尾" }, E(run));
    }

    [Fact]
    public async Task Running_fault_retains_run_and_closes_downtime_without_releasing_occupation()
    {
        var sample = await Setup();
        var run = await Start(sample);
        var detail = await Get($"equipment/tools/{Id(sample.Tool)}");
        Assert.Equal("Running", detail.GetProperty("states")[0].GetProperty("state").GetString());
        var down = await Ok($"equipment/tools/{Id(sample.Tool)}/state",
            new { state = "Down", reason = "故障" }, E(await EquipmentRow(sample)));
        Assert.Equal(Id(run),
            (await Get($"equipment/tools/{Id(sample.Tool)}")).GetProperty("currentRun").GetProperty("id").GetGuid());
        var downtime = (await Get($"equipment/downtimes?equipmentId={Id(sample.Tool)}")).GetProperty("items")[0];
        Assert.True(downtime.GetProperty("needsReason").GetBoolean());
        await Ok($"equipment/tools/{Id(sample.Tool)}/state", new { state = "Idle", reason = "故障排除" }, E(down));
        detail = await Get($"equipment/tools/{Id(sample.Tool)}");
        Assert.Equal("Running", detail.GetProperty("states")[0].GetProperty("state").GetString());
        await End(run);
        detail = await Get($"equipment/tools/{Id(sample.Tool)}");
        Assert.Equal("Idle", detail.GetProperty("states")[0].GetProperty("state").GetString());
        Assert.NotEqual(JsonValueKind.Null, (await Get($"equipment/downtimes?equipmentId={Id(sample.Tool)}"))
            .GetProperty("items")[0].GetProperty("endedAt").ValueKind);
    }

    [Fact]
    public async Task Down_at_trackout_requires_quality_disposition_instead_of_advancing()
    {
        var sample = await Setup();
        var run = await Start(sample);
        await Ok($"equipment/tools/{Id(sample.Tool)}/state", new { state = "Down", reason = "加工故障" },
            E(await EquipmentRow(sample)));
        var ended = await End(run);
        Assert.False(ended.GetProperty("advanced").GetBoolean());
        Assert.Equal("AwaitingDisposition", ended.GetProperty("lot").GetProperty("status").GetString());
        var lot = (await Get($"execution/lots/{Id(sample.Lot)}")).GetProperty("lot");
        await Ok($"quality/lots/{Id(lot)}/release", new { reason = "已审核结果", action = "Continue" }, E(lot));
        Assert.Equal("Ready",
            (await Get($"execution/lots/{Id(sample.Lot)}")).GetProperty("lot").GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
    }

    [Fact]
    public async Task Simulated_events_deduplicate_reject_old_states_and_separate_connection()
    {
        var sample = await Setup();
        var occurred = DateTime.UtcNow;
        var body = new
        {
            source = "test-simulator",
            sourceEventId = "1",
            occurredAt = occurred,
            kind = "State",
            state = "Down",
            reason = "模拟故障"
        };
        var first = await Send($"equipment/events/{Id(sample.Tool)}", body, E(await EquipmentRow(sample)), "event-key");
        Assert.True(first.IsSuccessStatusCode, await first.Content.ReadAsStringAsync());
        var row = await first.Content.ReadFromJsonAsync<JsonElement>();
        var replay = await Ok($"equipment/events/{Id(sample.Tool)}", body, E(await EquipmentRow(sample)));
        Assert.Equal(Id(row), Id(replay));
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"equipment/events/{Id(sample.Tool)}",
            new
            {
                source = "test-simulator",
                sourceEventId = "1",
                occurredAt = occurred,
                kind = "State",
                state = "Idle",
                reason = "异参"
            },
            E(await EquipmentRow(sample)))).StatusCode);
        var rejected = await Ok($"equipment/events/{Id(sample.Tool)}", new
        {
            source = "test-simulator",
            sourceEventId = "2",
            occurredAt = occurred.AddMinutes(-1),
            kind = "State",
            state = "Idle",
            reason = "旧事件"
        }, E(await EquipmentRow(sample)));
        Assert.Equal("Rejected", rejected.GetProperty("processingStatus").GetString());
        await Ok($"equipment/events/{Id(sample.Tool)}", new
        {
            source = "test-simulator",
            sourceEventId = "3",
            occurredAt = DateTime.UtcNow,
            kind = "Connection",
            state = "Online",
            reason = "联机模拟"
        }, E(await EquipmentRow(sample)));
        var equipment = await EquipmentRow(sample);
        Assert.Equal("Down", equipment.GetProperty("operationalState").GetString());
        Assert.Equal("Online", equipment.GetProperty("communicationState").GetString());
        Assert.Equal(1,
            (await Get($"equipment/downtimes?equipmentId={Id(sample.Tool)}")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Downtime_classification_and_statistics_share_one_interval()
    {
        var sample = await Setup();
        await Ok($"equipment/tools/{Id(sample.Tool)}/state", new { state = "Down", reason = "故障" },
            E(await EquipmentRow(sample)));
        var downtime = (await Get($"equipment/downtimes?equipmentId={Id(sample.Tool)}")).GetProperty("items")[0];
        var reason = await Ok("equipment/reasons", new { code = Guid.NewGuid().ToString("N"), name = "故障分类" });
        var shift = await Ok("equipment/shifts", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "日班",
            startTime = "08:00:00",
            endTime = "20:00:00"
        });
        await Ok($"equipment/downtimes/{Id(downtime)}/classify", new
        {
            reasonCodeId = Id(reason),
            shiftId = Id(shift),
            notes = "原因补录"
        }, E(downtime));
        Assert.Equal(HttpStatusCode.Conflict, (await Send("equipment/downtimes", new
        {
            equipmentStateId = downtime.GetProperty("equipmentStateId").GetGuid()
        }, E(await EquipmentRow(sample)))).StatusCode);
        await Ok($"equipment/tools/{Id(sample.Tool)}/state", new { state = "Idle", reason = "恢复" },
            E(await EquipmentRow(sample)));
        var stats = await Get($"equipment/statistics?equipmentId={Id(sample.Tool)}");
        var duration = stats.GetProperty("states").EnumerateArray().Single(x => x.GetProperty("state").GetString() == "Down").GetProperty("seconds").GetDouble();
        Assert.True(duration > 0);
        Assert.Equal(duration, stats.GetProperty("downtimes")[0].GetProperty("seconds").GetDouble());
        Assert.Equal(Id(shift), stats.GetProperty("downtimes")[0].GetProperty("shiftId").GetGuid());
    }

    [Fact]
    public async Task Due_maintenance_requires_start_complete_and_separate_release()
    {
        var sample = await Setup();
        var plan = await Ok("equipment/maintenance-plans", new
        {
            name = "周期维护",
            eventType = "PM",
            frequency = "Hours",
            intervalValue = 24,
            nextDueAt = DateTime.UtcNow.AddMinutes(-1),
            equipmentId = Id(sample.Tool)
        });
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
        var work = await Ok("equipment/maintenance", new
        {
            title = "维护",
            eventType = "PM",
            scheduledAt = DateTime.UtcNow,
            scheduleId = Id(plan)
        });
        work = await Ok($"equipment/maintenance/{Id(work)}/start", new { reason = "维护开始" }, E(work));
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
        work = await Ok($"equipment/maintenance/{Id(work)}/complete", new { reason = "维护完成" }, E(work));
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
        await Ok($"equipment/maintenance/{Id(work)}/release", new { reason = "复机确认" }, E(work));
        var currentPlan = (await Get($"equipment/maintenance-plans?id={Id(plan)}")).GetProperty("items")[0];
        Assert.True(currentPlan.GetProperty("nextDueAt").GetDateTime() > DateTime.UtcNow.AddHours(23));
        await Start(sample);
    }

    [Fact]
    public async Task Maintenance_cannot_start_while_running_and_defer_is_audited()
    {
        var sample = await Setup();
        var tool = await Ok("equipment/resources", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "维护工具",
            equipmentId = Id(sample.Tool)
        });
        var plan = await Ok("equipment/maintenance-plans", new
        {
            name = "工具维护",
            eventType = "PM",
            frequency = "Days",
            intervalValue = 1,
            nextDueAt = DateTime.UtcNow.AddDays(1),
            toolId = Id(tool)
        });
        Assert.Contains((await Get($"equipment/maintenance-plans?equipmentId={Id(sample.Tool)}"))
            .GetProperty("items").EnumerateArray(), row => Id(row) == Id(plan));
        var deferred = await Ok($"equipment/maintenance-plans/{Id(plan)}/defer",
            new { until = DateTime.UtcNow.AddDays(2), reason = "授权延期" }, E(plan));
        Assert.True(deferred.GetProperty("nextDueAt").GetDateTime() > plan.GetProperty("nextDueAt").GetDateTime());
        var run = await Start(sample);
        var work = await Ok("equipment/maintenance", new
        {
            title = "工具维护",
            eventType = "PM",
            scheduledAt = DateTime.UtcNow.AddDays(1),
            scheduleId = Id(plan)
        });
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"equipment/maintenance/{Id(work)}/start",
            new { reason = "开始" }, E(work))).StatusCode);
        await End(run);
        await Ok($"equipment/maintenance/{Id(work)}/start", new { reason = "已收尾" }, E(work));
        await using var db = f.Database.CreateContext();
        Assert.True(await db.Set<AuditLog>().AnyAsync(x => x.EntityId == Id(plan) && x.Action == "MaintenancePlanDeferred"));
    }

    [Fact]
    public async Task Qualifications_are_optional_until_enabled_and_worker_binding_is_required()
    {
        var sample = await Setup();
        await ResourcePolicy(sample, equipment: true, worker: true);
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
        var qualification = await Ok("equipment/equipment-qualifications", new
        {
            equipmentId = Id(sample.Tool),
            operationId = OperationOf(sample),
            validFrom = DateTime.UtcNow.AddDays(-1),
            validTo = DateTime.UtcNow.AddDays(1)
        });
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
        Guid workerId;
        await using (var db = f.Database.CreateContext())
        {
            var worker = new Worker { FabId = f.Fab, Code = Guid.NewGuid().ToString("N"), Name = "认证操作员" };
            db.Add(worker);
            var admin = await db.Users.SingleAsync(x => x.UserName == "admin");
            admin.WorkerId = worker.Id;
            workerId = worker.Id;
            await db.SaveChangesAsync();
        }
        await Ok("equipment/worker-qualifications", new
        {
            workerId,
            operationId = OperationOf(sample),
            validFrom = DateTime.UtcNow.AddDays(-1),
            validTo = DateTime.UtcNow.AddDays(1)
        });
        var run = await Start(sample);
        await End(run);
        await Ok($"equipment/equipment-qualifications/{Id(qualification)}/revoke", new { reason = "资格失效" },
            E(qualification));
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
        await Ok("equipment/equipment-qualifications", new
        {
            equipmentId = Id(sample.Tool),
            operationId = OperationOf(sample),
            validFrom = DateTime.UtcNow.AddSeconds(-1),
            validTo = DateTime.UtcNow.AddDays(1)
        });
        await Start(sample);
    }

    [Fact]
    public async Task Specific_expired_qualification_cannot_fall_back_to_general_qualification()
    {
        var sample = await Setup();
        await ResourcePolicy(sample, equipment: true);
        await Ok("equipment/equipment-qualifications", new
        {
            equipmentId = Id(sample.Tool),
            operationId = OperationOf(sample),
            validFrom = DateTime.UtcNow.AddDays(-2)
        });
        await Ok("equipment/equipment-qualifications", new
        {
            equipmentId = Id(sample.Tool),
            operationId = OperationOf(sample),
            recipeVersionId = Id(sample.Recipe),
            validFrom = DateTime.UtcNow.AddDays(-2),
            validTo = DateTime.UtcNow.AddDays(-1)
        });
        var blocked = await TryResourceStart(sample);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Contains("设备工艺资格", await blocked.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Reticle_life_is_consumed_once_and_actual_wafers_survive_unloading()
    {
        var sample = await Setup(count: 2);
        await ResourcePolicy(sample, layer: "L1");
        var reticle = await Ok("equipment/reticles", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "光罩",
            revision = "A",
            layerCode = "L1",
            maxUsageCount = 1
        });
        var mount = await Ok($"equipment/reticle-mounts/{Id(sample.Tool)}", new { reticleId = Id(reticle) },
            E(await EquipmentRow(sample)));
        var run = await Start(sample);
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"equipment/reticle-mounts/{Id(mount)}/unload",
            new { reason = "加工中拒绝" }, E(mount))).StatusCode);
        await End(run);
        var impact = await Get($"equipment/reticles/{Id(reticle)}/impact");
        Assert.Equal(1, impact.GetProperty("total").GetInt32());
        Assert.Equal(2, impact.GetProperty("items")[0].GetProperty("wafers").GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
        await Ok($"equipment/reticle-mounts/{Id(mount)}/unload", new { reason = "寿命用尽" }, E(mount));
        Assert.Equal(1,
            (await Get($"equipment/reticles?id={Id(reticle)}")).GetProperty("items")[0].GetProperty("usageCount").GetInt64());
        Assert.Equal(1, (await Get($"equipment/reticles/{Id(reticle)}/impact")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Consumable_lifetime_required_mount_and_run_impact_do_not_change_inventory()
    {
        var sample = await Setup();
        var material = await Ok("equipment/consumables", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "化学品",
            unit = "L"
        });
        var lot = await Ok("equipment/consumable-lots", new
        {
            materialId = Id(material),
            lotNumber = Guid.NewGuid().ToString("N"),
            expiryAt = DateTime.UtcNow.AddDays(2)
        });
        await ResourcePolicy(sample, materials: [Id(material)]);
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send($"equipment/material-mounts/{Id(sample.Tool)}",
            new
            {
                materialLotId = Id(lot),
                openedAt = DateTime.UtcNow.AddHours(-1),
                usableUntil = DateTime.UtcNow.AddDays(3)
            }, E(await EquipmentRow(sample)))).StatusCode);
        var mount = await Ok($"equipment/material-mounts/{Id(sample.Tool)}", new
        {
            materialLotId = Id(lot),
            openedAt = DateTime.UtcNow.AddHours(-1),
            usableUntil = DateTime.UtcNow.AddDays(1)
        },
            E(await EquipmentRow(sample)));
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"equipment/material-mounts/{Id(sample.Tool)}",
            new
            {
                materialLotId = Id(lot),
                openedAt = DateTime.UtcNow.AddHours(-1),
                usableUntil = DateTime.UtcNow.AddDays(1)
            }, E(await EquipmentRow(sample)))).StatusCode);
        await End(await Start(sample));
        await Ok($"equipment/material-mounts/{Id(mount)}/unload", new { reason = "换料" }, E(mount));
        Assert.Equal(1, (await Get($"equipment/material-mounts/{Id(mount)}/impact")).GetProperty("total").GetInt32());
        var currentLot = (await Get($"equipment/consumable-lots?id={Id(lot)}")).GetProperty("items")[0];
        Assert.Equal(0, currentLot.GetProperty("quantityAvailable").GetDecimal());
        Assert.Equal(0, currentLot.GetProperty("quantityReceived").GetDecimal());
    }

    [Fact]
    public async Task Equipment_new_permissions_and_foreign_resource_scope_are_enforced()
    {
        var sample = await Setup();
        var (viewer, _, _) = await f.NewUser();
        Assert.Equal(HttpStatusCode.Forbidden, (await Send($"equipment/events/{Id(sample.Tool)}", new { },
            client: viewer)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/api/v1/equipment/reticles")).StatusCode);
        await using var db = f.Database.CreateContext();
        var foreign = new Fab { Code = Guid.NewGuid().ToString("N"), Name = "外厂", Timezone = "Asia/Shanghai" };
        var reticle = new Reticle
        {
            FabId = foreign.Id,
            Code = Guid.NewGuid().ToString("N"),
            Name = "外厂光罩",
            Revision = "A",
            LayerCode = "L",
            Status = "Available"
        };
        db.AddRange(foreign, reticle);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound,
            (await f.Admin.GetAsync($"/api/v1/equipment/reticles/{reticle.Id}/impact")).StatusCode);
    }

    [Fact]
    public async Task Resource_expiry_boundary_and_active_material_holds_block_future_starts()
    {
        var sample = await Setup();
        var material = await Ok("equipment/consumables", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "时限耗材",
            unit = "L"
        });
        var lot = await Ok("equipment/consumable-lots", new
        {
            materialId = Id(material),
            lotNumber = Guid.NewGuid().ToString("N")
        });
        var deadline = DateTime.UtcNow.AddHours(1);
        await Ok($"equipment/material-mounts/{Id(sample.Tool)}", new
        {
            materialLotId = Id(lot),
            openedAt = DateTime.UtcNow.AddMinutes(-1),
            usableUntil = deadline
        }, E(await EquipmentRow(sample)));
        var user = (await Get("auth/me")).GetProperty("userId").GetGuid();
        await using var db = f.Database.CreateContext();
        Assert.Empty(await EquipmentResources.Blocks(db, f.Fab, user, Id(sample.Tool),
            OperationOf(sample), Id(sample.Recipe), deadline.AddTicks(-10)));
        Assert.NotEmpty(await EquipmentResources.Blocks(db, f.Fab, user, Id(sample.Tool),
            OperationOf(sample), Id(sample.Recipe), deadline.AddTicks(10)));
        var reason = await db.Set<ReasonCode>().FirstAsync(x => x.Category == "Hold");
        db.Add(new MaterialHold
        {
            MaterialLotId = Id(lot),
            ReasonCodeId = reason.Id,
            HeldAt = DateTime.UtcNow,
            HeldById = user
        });
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await TryResourceStart(sample)).StatusCode);
    }

    [Fact]
    public async Task Aborted_run_keeps_consumed_reticle_life_and_idempotency_does_not_duplicate_usage()
    {
        var sample = await Setup();
        var reticle = await Ok("equipment/reticles", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "中止光罩",
            revision = "A",
            layerCode = "L1",
            maxUsageCount = 2
        });
        await Ok($"equipment/reticle-mounts/{Id(sample.Tool)}", new { reticleId = Id(reticle) },
            E(await EquipmentRow(sample)));
        var lot = (await Get($"execution/lots/{Id(sample.Lot)}")).GetProperty("lot");
        var equipment = await EquipmentRow(sample);
        var body = new
        {
            equipmentId = Id(equipment),
            equipmentEtag = E(equipment),
            recipeVersionId = Id(sample.Recipe)
        };
        var key = Guid.NewGuid().ToString();
        var path = $"execution/lots/{Id(lot)}/track-in";
        var first = await Send(path, body, E(lot), key);
        Assert.True(first.IsSuccessStatusCode);
        var run = (await first.Content.ReadFromJsonAsync<JsonElement>());
        Assert.True((await Send(path, body, E(lot), key)).IsSuccessStatusCode);
        await Ok($"execution/runs/{Id(run)}/abort", new { reason = "受控中止" }, E(run));
        await using var db = f.Database.CreateContext();
        Assert.Equal(1L, (await db.Set<Reticle>().SingleAsync(x => x.Id == Id(reticle))).UsageCount);
        Assert.Equal(1, await db.Set<ReticleUsage>().CountAsync(x => x.ReticleId == Id(reticle)));
        Assert.NotNull((await db.Set<ReticleUsage>().SingleAsync(x => x.ReticleId == Id(reticle))).UnloadedAt);
    }

#pragma warning disable EF1002
    [Fact]
    public async Task Equipment_runtime_grants_support_resource_maintenance_event_and_qualification_commands()
    {
        var sample = await Setup();
        var user = (await Get("auth/me")).GetProperty("userId").GetGuid();
        var role = "mesagent_test_f7_" + Guid.NewGuid().ToString("N");
        await using var db = f.Database.CreateContext();
        await db.Database.ExecuteSqlRawAsync($"CREATE ROLE {role} NOLOGIN");
        try
        {
            await db.Database.ExecuteSqlRawAsync($"""
                GRANT USAGE ON SCHEMA organization,security,engineering,production,equipment,quality,audit,materials TO {role};
                GRANT SELECT ON ALL TABLES IN SCHEMA organization,security,engineering,production,equipment,quality,audit,materials TO {role};
                GRANT INSERT ON audit.audit_logs,audit.business_events,audit.command_receipts TO {role};
                """);
            var grants = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,
                "equipment-runtime-grants.sql"));
            await db.Database.ExecuteSqlRawAsync(grants.Replace("mesagent_app", role));
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync($"SET LOCAL ROLE {role}");
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(78001001)");
            var backend = new EquipmentBackend(db, f.Fab, new RequestActor(user, Guid.NewGuid(), Guid.NewGuid()),
                p => Assert.Contains(p, PermissionCatalog.All), TimeProvider.System);
            async Task<JsonElement> Execute(string path, object body, string? version = null)
            {
                var result = await backend.Execute(new("POST", path, JsonSerializer.SerializeToElement(body),
                    new Dictionary<string, string>(), version, Guid.NewGuid().ToString()));
                await db.SaveChangesAsync();
                return JsonSerializer.SerializeToElement(result.Data);
            }
            var plan = await Execute("equipment/maintenance-plans", new
            {
                name = "权限测试",
                eventType = "PM",
                frequency = "Days",
                intervalValue = 1,
                nextDueAt = DateTime.UtcNow,
                equipmentId = Id(sample.Tool)
            });
            var work = await Execute("equipment/maintenance", new
            {
                title = "权限维护",
                eventType = "PM",
                scheduledAt = DateTime.UtcNow,
                scheduleId = Id(plan)
            });
            work = await Execute($"equipment/maintenance/{Id(work)}/start", new { reason = "开始" }, E(work));
            work = await Execute($"equipment/maintenance/{Id(work)}/complete", new { reason = "完成" }, E(work));
            await Execute($"equipment/maintenance/{Id(work)}/release", new { reason = "复机" }, E(work));
            var material = await Execute("equipment/consumables", new
            {
                code = Guid.NewGuid().ToString("N"),
                name = "权限耗材",
                unit = "L"
            });
            await Execute("equipment/consumable-lots", new
            {
                materialId = Id(material),
                lotNumber = Guid.NewGuid().ToString("N")
            });
            await Execute("equipment/equipment-qualifications", new
            {
                equipmentId = Id(sample.Tool),
                operationId = OperationOf(sample),
                validFrom = DateTime.UtcNow.AddDays(-1)
            });
            var down = await Execute($"equipment/tools/{Id(sample.Tool)}/state", new
            {
                state = "Down",
                reason = "权限停机"
            }, E(sample.Tool));
            await Execute($"equipment/tools/{Id(sample.Tool)}/state", new { state = "Idle", reason = "权限恢复" }, E(down));
            Assert.False(await db.Database.SqlQueryRaw<bool>(
                "SELECT has_schema_privilege(current_user,'equipment','CREATE') AS \"Value\"").SingleAsync());
            Assert.False(await db.Database.SqlQueryRaw<bool>(
                "SELECT has_table_privilege(current_user,'equipment.material_run_usages','DELETE') AS \"Value\"").SingleAsync());
            await tx.RollbackAsync();
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync($"DROP OWNED BY {role}");
            await db.Database.ExecuteSqlRawAsync($"DROP ROLE {role}");
        }
    }
#pragma warning restore EF1002

    [Fact]
    public async Task Qualification_policy_cannot_enable_only_one_side()
    {
        var sample = await Setup();
        var response = await Send("equipment/policies", new
        {
            operationId = OperationOf(sample),
            requireEquipmentQualification = true,
            requireWorkerQualification = false,
            requiredMaterialIds = Array.Empty<Guid>()
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_trackin_and_reticle_unload_cannot_create_half_a_resource_use()
    {
        var sample = await Setup();
        await ResourcePolicy(sample, layer: "L1");
        var reticle = await Ok("equipment/reticles", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "并发光罩",
            revision = "A",
            layerCode = "L1",
            maxUsageCount = 2
        });
        var mount = await Ok($"equipment/reticle-mounts/{Id(sample.Tool)}", new { reticleId = Id(reticle) },
            E(await EquipmentRow(sample)));
        var equipment = await EquipmentRow(sample);
        var lot = (await Get($"execution/lots/{Id(sample.Lot)}")).GetProperty("lot");
        var results = await Task.WhenAll(
            Send($"execution/lots/{Id(lot)}/track-in", new
            {
                equipmentId = Id(equipment),
                equipmentEtag = E(equipment),
                recipeVersionId = Id(sample.Recipe)
            }, E(lot)),
            Send($"equipment/reticle-mounts/{Id(mount)}/unload", new { reason = "并发卸下" }, E(mount)));
        Assert.Single(results, x => x.IsSuccessStatusCode);
        await using var db = f.Database.CreateContext();
        var usages = await db.Set<ReticleUsage>().CountAsync(x => x.ReticleId == Id(reticle));
        var current = await db.Set<Reticle>().SingleAsync(x => x.Id == Id(reticle));
        Assert.Equal(results[0].IsSuccessStatusCode ? 1 : 0, usages);
        Assert.Equal(usages, current.UsageCount);
    }

    [Fact]
    public async Task Legacy_unregistered_down_interval_can_be_registered_once_without_rewriting_time()
    {
        var sample = await Setup();
        Guid equipmentId;
        Guid stateId;
        await using (var db = f.Database.CreateContext())
        {
            var source = await db.Set<Equipment>().SingleAsync(x => x.Id == Id(sample.Tool));
            var equipment = new Equipment
            {
                Code = Guid.NewGuid().ToString("N"),
                Name = "历史设备",
                AreaId = source.AreaId,
                EquipmentGroupId = source.EquipmentGroupId,
                EquipmentTypeId = source.EquipmentTypeId,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                OperationalState = "Idle"
            };
            var state = new EquipmentState
            {
                EquipmentId = equipment.Id,
                State = "Down",
                StartedAt = DateTime.UtcNow.AddHours(-2),
                EndedAt = DateTime.UtcNow.AddHours(-1),
                Source = "Legacy"
            };
            db.AddRange(equipment, state, new EquipmentState
            {
                EquipmentId = equipment.Id,
                State = "Idle",
                StartedAt = state.EndedAt.Value,
                Source = "Legacy"
            });
            await db.SaveChangesAsync();
            equipmentId = equipment.Id;
            stateId = state.Id;
        }
        var states = await Get($"equipment/states?equipmentId={equipmentId}");
        var old = states.GetProperty("items").EnumerateArray().Single(x => Id(x) == stateId);
        Assert.Equal(JsonValueKind.Null, old.GetProperty("downtimeId").ValueKind);
        var current = (await Get($"equipment/tools/{equipmentId}")).GetProperty("equipment");
        var downtime = await Ok("equipment/downtimes", new { equipmentStateId = stateId }, E(current));
        Assert.Equal(old.GetProperty("startedAt").GetDateTime(), downtime.GetProperty("startedAt").GetDateTime());
        Assert.Equal(old.GetProperty("endedAt").GetDateTime(), downtime.GetProperty("endedAt").GetDateTime());
        Assert.True(downtime.GetProperty("needsReason").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await Send("equipment/downtimes",
            new { equipmentStateId = stateId }, E(current))).StatusCode);
        var registered = (await Get($"equipment/states?equipmentId={equipmentId}"))
            .GetProperty("items").EnumerateArray().Single(x => Id(x) == stateId);
        Assert.Equal(Id(downtime), registered.GetProperty("downtimeId").GetGuid());
    }
}
