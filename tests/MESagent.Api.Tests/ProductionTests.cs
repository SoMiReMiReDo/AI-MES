using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.LotWaferCarrierGenealogy;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MESagent.Api.Tests;

public sealed class ProductionTests(OrganizationIdentityAccessControlFixture f)
    : IClassFixture<OrganizationIdentityAccessControlFixture>
{
    private static string E(JsonElement value) => value.GetProperty("etag").GetString()!;
    private static Guid Id(JsonElement value) => value.GetProperty("id").GetGuid();
    private async Task<JsonElement> Get(string path) => await f.Admin.GetFromJsonAsync<JsonElement>("/api/v1/" + path);

    private async Task<HttpResponseMessage> Send(string path, object body, string method = "POST",
        string? etag = null, string? key = null, HttpClient? client = null)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/" + path) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        if (etag != null)
            request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await (client ?? f.Admin).SendAsync(request);
    }

    private async Task<JsonElement> Ok(string path, object body, string method = "POST", string? etag = null)
    {
        var response = await Send(path, body, method, etag);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> Order(int quantity = 5)
    {
        var code = Guid.NewGuid().ToString("N");
        var product = await Ok("engineering/products", new { code, name = "F4 产品", unitOfMeasure = "wafer" });
        var route = await Ok("engineering/routes", new { code, name = "F4 路线", productId = Id(product) });
        var version = await Ok("engineering/route-versions", new { routeId = Id(route), version = "1" });
        var operation = await Ok("engineering/operations", new { code, name = "测试工序", segmentType = "Process" });
        await Ok("engineering/route-steps", new
        {
            routeVersionId = Id(version),
            operationId = Id(operation),
            sequenceNo = 10,
            name = "终点测试站"
        });
        await Ok($"engineering/route-versions/{Id(version)}/release", new { reason = "F4 测试" }, etag: E(version));
        var revision = await Ok("engineering/product-revisions", new
        {
            productId = Id(product),
            routeVersionId = Id(version),
            revisionCode = "A"
        });
        await Ok($"engineering/product-revisions/{Id(revision)}/release", new { reason = "发布" }, etag: E(revision));
        var order = await Ok("planning/work-orders", new
        {
            orderNo = code,
            productId = Id(product),
            productRevisionId = Id(revision),
            plannedQty = quantity
        });
        return await Ok($"planning/work-orders/{Id(order)}/release", new { reason = "下达" }, etag: E(order));
    }

    private async Task<JsonElement> Lot(JsonElement order, int count = 5, string? code = null) =>
        await Ok("production/lots", new
        {
            workOrderId = Id(order),
            lotNumber = code ?? Guid.NewGuid().ToString("N"),
            wafers = Enumerable.Range(1, count).Select(_ => new { waferCode = Guid.NewGuid().ToString("N"), waferSizeMm = 300 })
        });

    private async Task<JsonElement> Release(JsonElement lot) => await Ok($"production/lots/{Id(lot)}/release",
        new { }, etag: E(lot));

    private async Task<JsonElement> Carrier(int capacity = 5) => await Ok("production/carriers", new
    {
        carrierCode = Guid.NewGuid().ToString("N"),
        carrierType = "Demo",
        slotCapacity = capacity,
        allowedWaferSizeMm = 300
    });

    [Fact]
    public async Task Create_release_split_merge_preserves_identity_membership_and_first_release()
    {
        var order = await Order();
        var original = await Release(await Lot(order));
        var id = Id(original);
        var wafers = (await Get($"production/lots/{id}/wafers")).GetProperty("items").EnumerateArray().ToArray();
        var selected = wafers.Take(2).Select(Id).ToArray();
        var splitBody = new { waferIds = selected, reason = "2+3", newLotNumber = Guid.NewGuid().ToString("N") };
        var preview = await Ok($"production/lots/{id}/split-preview", splitBody);
        Assert.Equal(3, preview.GetProperty("remainingCount").GetInt32());
        var split = await Ok($"production/lots/{id}/split", splitBody, etag: E(original));
        var child = split.GetProperty("newLot");
        var parent = await Get($"production/lots/{id}");
        var remaining = (await Get($"production/lots/{id}/wafers")).GetProperty("items").EnumerateArray().Select(Id).ToArray();
        var merge = await Ok("production/lot-merges", new
        {
            sources = new[]
            {
                new { lotId = id, etag = E(parent), waferIds = remaining },
                new { lotId = Id(child), etag = E(child), waferIds = selected }
            },
            reason = "合回",
            newLotNumber = Guid.NewGuid().ToString("N")
        });
        var merged = merge.GetProperty("newLot");
        await using var db = f.Database.CreateContext();
        Assert.Equal(5, await db.Set<WaferRelease>().CountAsync(x => x.WorkOrderId == Id(order)));
        Assert.Equal(5, await db.Set<LotMembership>().CountAsync(x => x.LotId == Id(merged) && x.ValidTo == null));
        Assert.Equal(0, await db.Set<LotMembership>().CountAsync(x => x.LotId == id && x.ValidTo == null));
        Assert.Equal("Merged", (await db.Set<Lot>().FindAsync(id))!.Status);
        Assert.Equal(3, await db.Set<StepExecutionInheritance>().CountAsync(x =>
            x.TargetStepExecutionId == db.Set<StepExecution>().Single(s => s.LotId == Id(merged)).Id
            || x.TargetStepExecutionId == db.Set<StepExecution>().Single(s => s.LotId == Id(child)).Id));
        var genealogy = await Get($"production/lots/{Id(merged)}/genealogy?direction=ancestors&depth=10");
        Assert.Equal(3, genealogy.GetProperty("total").GetInt32());
        var history = await Get($"production/wafers/{selected[0]}/history");
        Assert.True(history.GetProperty("total").GetInt32() >= 5);
        Assert.Equal(0, await db.Set<WaferCompletion>().CountAsync(x => x.WorkOrderId == Id(order)));
    }

    [Fact]
    public async Task Replay_is_canonical_and_stale_version_does_not_block_successful_retry()
    {
        var order = await Order();
        var code = Guid.NewGuid().ToString("N");
        var waferCode = Guid.NewGuid().ToString("N");
        var key = Guid.NewGuid().ToString();
        var first = await Send("production/lots", new
        {
            workOrderId = Id(order),
            lotNumber = code,
            wafers = new[] { new { waferCode, waferSizeMm = 300 } }
        }, key: key);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var replay = await Send("production/lots", new
        {
            wafers = new[] { new { waferSizeMm = 300.0m, waferCode } },
            lotNumber = code,
            workOrderId = Id(order)
        }, key: key);
        Assert.True(replay.Headers.Contains("Idempotency-Replayed"));
        var lot = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Id(lot), Id(await replay.Content.ReadFromJsonAsync<JsonElement>()));
        var releaseKey = Guid.NewGuid().ToString();
        Assert.Equal(HttpStatusCode.OK, (await Send($"production/lots/{Id(lot)}/release",
            new { }, etag: E(lot), key: releaseKey)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send($"production/lots/{Id(lot)}/release",
            new { }, etag: E(lot), key: releaseKey)).StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionFailed,
            (await Send($"production/lots/{Id(lot)}/release", new { }, etag: E(lot))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await Send("production/lots", new { workOrderId = Id(order) }, key: key)).StatusCode);
    }

    [Fact]
    public async Task Concurrent_release_cannot_exceed_work_order_quantity()
    {
        var order = await Order(1);
        var a = await Lot(order, 1);
        var b = await Lot(order, 1);
        var responses = await Task.WhenAll(
            Send($"production/lots/{Id(a)}/release", new { }, etag: E(a)),
            Send($"production/lots/{Id(b)}/release", new { }, etag: E(b)));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
    }

    private async Task<object> LoadBody(Guid lotId, Guid[] waferIds, (Guid? From, Guid? To)[] moves, Guid[] carrierIds)
    {
        var carriers = new List<object>();
        foreach (var id in carrierIds)
        {
            var row = await Get($"production/carriers/{id}");
            carriers.Add(new { id, etag = E(row) });
        }
        var wafers = new List<object>();
        foreach (var id in waferIds)
        {
            var row = await Get($"production/wafers/{id}");
            wafers.Add(new { id, etag = E(row) });
        }
        var lot = await Get($"production/lots/{lotId}");
        return new
        {
            carrierVersions = carriers,
            waferVersions = wafers,
            lotVersions = new[] { new { id = lotId, etag = E(lot) } },
            moves = waferIds.Select((id, i) => new { waferId = id, fromSlotId = moves[i].From, toSlotId = moves[i].To }),
            reason = "装载测试"
        };
    }

    [Fact]
    public async Task Carrier_swap_is_atomic_and_cancel_closes_membership_and_loads()
    {
        var lot = await Lot(await Order(2), 2);
        var wafers = lot.GetProperty("waferIds").EnumerateArray().Select(x => x.GetGuid()).ToArray();
        var carrier = await Carrier(2);
        var slots = (await Get($"production/carriers/{Id(carrier)}")).GetProperty("slots").EnumerateArray().Select(Id).ToArray();
        await Ok("production/carrier-load-commands", await LoadBody(Id(lot), wafers,
            [(null, slots[0]), (null, slots[1])], [Id(carrier)]));
        await Ok("production/carrier-load-commands", await LoadBody(Id(lot), wafers,
            [(slots[0], slots[1]), (slots[1], slots[0])], [Id(carrier)]));
        var currentCarrier = await Get($"production/carriers/{Id(carrier)}");
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"production/carriers/{Id(carrier)}",
            new { status = "Disabled" }, "PUT", E(currentCarrier))).StatusCode);
        var current = await Get($"production/lots/{Id(lot)}");
        await Ok($"production/lots/{Id(lot)}/cancel", new { reason = "登记取消" }, etag: E(current));
        await using var db = f.Database.CreateContext();
        Assert.Equal(4, await db.Set<CarrierLoad>().CountAsync(x => wafers.Contains(x.WaferId)));
        Assert.False(await db.Set<CarrierLoad>().AnyAsync(x => wafers.Contains(x.WaferId) && x.UnloadedAt == null));
        Assert.False(await db.Set<LotMembership>().AnyAsync(x => x.LotId == Id(lot) && x.ValidTo == null));
        Assert.All(await db.Set<Wafer>()
            .Where(x => wafers.Contains(x.Id)).ToListAsync(), x => Assert.Equal("Cancelled", x.LifecycleStatus));
    }

    [Fact]
    public async Task Occupied_slot_conflict_rolls_back_and_preserves_all_versions()
    {
        var lot = await Lot(await Order(2), 2);
        var wafers = lot.GetProperty("waferIds").EnumerateArray().Select(x => x.GetGuid()).ToArray();
        var carrier = await Carrier(1);
        var slot = Id((await Get($"production/carriers/{Id(carrier)}")).GetProperty("slots")[0]);
        var bodyA = await LoadBody(Id(lot), [wafers[0]], [(null, slot)], [Id(carrier)]);
        var bodyB = await LoadBody(Id(lot), [wafers[1]], [(null, slot)], [Id(carrier)]);
        var responses = await Task.WhenAll(Send("production/carrier-load-commands", bodyA), Send("production/carrier-load-commands", bodyB));
        Assert.Single(responses, x => x.IsSuccessStatusCode);
        Assert.Single(responses, x => x.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.PreconditionFailed);
        await using var db = f.Database.CreateContext();
        Assert.Equal(1, await db.Set<CarrierLoad>().CountAsync(x => x.CarrierSlotId == slot && x.UnloadedAt == null));
    }

    [Fact]
    public async Task Duplicate_lot_number_during_split_rolls_back_all_closed_intervals()
    {
        var order = await Order();
        var lot = await Release(await Lot(order));
        var wafers = (await Get($"production/lots/{Id(lot)}/wafers")).GetProperty("items").EnumerateArray().Select(Id).ToArray();
        var result = await Send($"production/lots/{Id(lot)}/split", new
        {
            waferIds = wafers.Take(2),
            reason = "重号回滚",
            newLotNumber = lot.GetProperty("lotNumber").GetString()
        }, etag: E(lot));
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        await using var db = f.Database.CreateContext();
        Assert.Equal(5, await db.Set<LotMembership>().CountAsync(x => x.LotId == Id(lot) && x.ValidTo == null));
        Assert.Equal(1, await db.Set<Lot>().CountAsync(x => x.WorkOrderId == Id(order)));
        Assert.False(await db.Set<GenealogyDetail>().AnyAsync(x => x.FromLotId == Id(lot)));
    }

    [Fact]
    public async Task Holds_forbid_split_and_releasing_one_of_two_does_not_unblock()
    {
        var lot = await Release(await Lot(await Order()));
        var waferId = Id((await Get($"production/lots/{Id(lot)}/wafers")).GetProperty("items")[0]);
        await using var db = f.Database.CreateContext();
        var user = (await Get("auth/me")).GetProperty("userId").GetGuid();
        var reason = new ReasonCode { Code = Guid.NewGuid().ToString(), Name = "测试", Category = "Hold" };
        var a = new HoldRecord { LotId = Id(lot), ReasonCodeId = reason.Id, HeldAt = DateTime.UtcNow, HeldById = user, HoldScope = "Lot" };
        var b = new HoldRecord { LotId = Id(lot), ReasonCodeId = reason.Id, HeldAt = DateTime.UtcNow.AddSeconds(1), HeldById = user, HoldScope = "Lot" };
        db.AddRange(reason, a, b);
        await db.SaveChangesAsync();
        var body = new { waferIds = new[] { waferId }, reason = "测试", newLotNumber = Guid.NewGuid().ToString() };
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"production/lots/{Id(lot)}/split", body, etag: E(lot))).StatusCode);
        a.ReleasedAt = DateTime.UtcNow.AddSeconds(2);
        a.ReleasedById = user;
        a.ReleaseReason = "解除一条";
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"production/lots/{Id(lot)}/split", body, etag: E(lot))).StatusCode);
    }

    [Fact]
    public async Task Unauthorized_users_and_cross_fab_objects_do_not_leak()
    {
        var lot = await Lot(await Order());
        var user = await f.NewUser();
        Assert.Equal(HttpStatusCode.Forbidden, (await user.Client.GetAsync("/api/v1/production/lots")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send("production/lots",
            new { }, client: user.Client)).StatusCode);
        var foreign = await f.Database.SeedAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync($"/api/v1/production/lots/{foreign.Lot}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync($"/api/v1/production/wafers/{foreign.Wafer}")).StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await Send($"production/lots/{Id(lot)}/release",
            new { })).StatusCode);
    }

    [Fact]
    public async Task Invalid_fields_sets_sizes_and_cross_order_merge_are_rejected()
    {
        var order = await Order();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send("production/lots",
            new { workOrderId = Id(order), wafers = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send("production/lots",
            new { workOrderId = Id(order), status = "Completed", wafers = Array.Empty<object>() })).StatusCode);
        var lot = await Release(await Lot(order));
        var other = await Release(await Lot(await Order()));
        var sources = new List<object>();
        foreach (var row in new[] { lot, other })
            sources.Add(new
            {
                lotId = Id(row),
                etag = E(row),
                waferIds =
                (await Get($"production/lots/{Id(row)}/wafers")).GetProperty("items").EnumerateArray().Select(Id).ToArray()
            });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send("production/lot-merges",
            new { sources, reason = "跨工单" })).StatusCode);
    }
    [Fact]
    public async Task Terminal_completion_requires_real_run_is_unique_and_has_no_public_bypass()
    {
        var order = await Order(2);
        var lot = await Release(await Lot(order, 2));
        var actor = (await Get("auth/me")).GetProperty("userId").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound,
            (await Send($"production/lots/{Id(lot)}/complete", new { }, etag: E(lot))).StatusCode);
        await using var db = f.Database.CreateContext();
        var step = await db.Set<StepExecution>().SingleAsync(x => x.LotId == Id(lot));
        var area = new Area { FabId = f.Fab, Code = Guid.NewGuid().ToString(), Name = "完成验证区域" };
        var type = new EquipmentType { Code = Guid.NewGuid().ToString(), Name = "验证机型" };
        var group = new EquipmentGroup { FabId = f.Fab, Code = Guid.NewGuid().ToString(), Name = "验证设备组" };
        var equipment = new Equipment
        {
            Code = Guid.NewGuid().ToString(),
            Name = "验证设备",
            AreaId = area.Id,
            EquipmentTypeId = type.Id,
            EquipmentGroupId = group.Id,
            OperationalState = "Idle"
        };
        var recipe = new Recipe { Code = Guid.NewGuid().ToString(), Name = "验证配方" };
        var version = new RecipeVersion { RecipeId = recipe.Id, Version = "1", LifecycleStatus = "Released" };
        var run = new ProcessRun
        {
            StepExecutionId = step.Id,
            EquipmentId = equipment.Id,
            RecipeVersionId = version.Id,
            TrackInAt = DateTime.UtcNow,
            TrackInById = actor,
            Status = "Running"
        };
        db.AddRange(area, type, group, equipment, recipe, version, run);
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(78001001)");
        var backend = new ProductionBackend(db, f.Fab, new RequestActor(actor, Guid.NewGuid(), Guid.NewGuid()),
            permission => Assert.Equal("execution.track-out", permission), _ => true, TimeProvider.System);
        var command = Guid.NewGuid();
        Assert.Equal("LotStateConflict", (await Assert.ThrowsAsync<AppException>(() =>
            backend.CompleteAtRouteEnd(run.Id, command))).Code);
        run.Status = "Completed";
        run.TrackOutAt = DateTime.UtcNow.AddSeconds(1);
        run.TrackOutById = actor;
        step.Status = "Completed";
        step.ExitedAt = run.TrackOutAt;
        var wafers = await db.Set<Wafer>().Where(x => x.WorkOrderId == Id(order)).ToListAsync();
        foreach (var wafer in wafers)
            db.Add(new ProcessRunWafer
            {
                ProcessRunId = run.Id,
                WaferId = wafer.Id,
                Result = "Passed",
                ProcessedAt = run.TrackOutAt
            });
        await db.SaveChangesAsync();
        await backend.CompleteAtRouteEnd(run.Id, command);
        await backend.CompleteAtRouteEnd(run.Id, command);
        await tx.CommitAsync();
        Assert.Equal(2, await db.Set<WaferCompletion>().CountAsync(x => x.WorkOrderId == Id(order)));
        Assert.Equal(2, (await db.Set<WorkOrder>().FindAsync(Id(order)))!.ProducedQty);
        Assert.Equal(2, await db.Set<LotMembership>().CountAsync(x => x.LotId == Id(lot) && x.ValidTo == null));
        Assert.All(wafers, x => Assert.Equal("Completed", x.LifecycleStatus));
        await using var verify = f.Database.CreateContext();
        var completion = await verify.Set<WaferCompletion>().FirstAsync(x => x.WorkOrderId == Id(order));
        completion.CompletedAt = completion.CompletedAt.AddDays(1);
        await Assert.ThrowsAsync<DbUpdateException>(() => verify.SaveChangesAsync());
    }

    [Fact]
    public async Task Scrap_collaboration_closes_members_and_records_all_scrapped_without_completion()
    {
        var order = await Order(1);
        var lot = await Release(await Lot(order, 1));
        var waferId = Id((await Get($"production/lots/{Id(lot)}/wafers")).GetProperty("items")[0]);
        var actor = (await Get("auth/me")).GetProperty("userId").GetGuid();
        await using var db = f.Database.CreateContext();
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(78001001)");
        var reason = new ReasonCode { Code = Guid.NewGuid().ToString(), Name = "验证报废", Category = "Scrap" };
        var scrap = new ScrapEntry
        {
            WorkOrderId = Id(order),
            ReasonCodeId = reason.Id,
            Quantity = 1,
            ReportedById = actor,
            ReportedAt = DateTime.UtcNow
        };
        var disposition = new WaferDisposition
        {
            WaferId = waferId,
            LotIdAtEvent = Id(lot),
            ScrapEntryId = scrap.Id,
            DispositionType = "Scrap",
            ReasonCodeId = reason.Id,
            ApprovedById = actor,
            PerformedById = actor,
            OccurredAt = DateTime.UtcNow
        };
        db.AddRange(reason, scrap, disposition);
        await db.SaveChangesAsync();
        var backend = new ProductionBackend(db, f.Fab, new RequestActor(actor, Guid.NewGuid(), Guid.NewGuid()),
            permission => Assert.Equal("quality.scrap", permission), _ => true, TimeProvider.System);
        await backend.CloseMembershipForScrap(disposition.Id, Guid.NewGuid());
        await tx.CommitAsync();
        var closed = await db.Set<Lot>().FindAsync(Id(lot));
        Assert.Equal("Closed", closed!.Status);
        Assert.Equal("AllScrapped", closed.CloseReason);
        Assert.Null(closed.CompletedAt);
        Assert.False(await db.Set<LotMembership>().AnyAsync(x => x.LotId == Id(lot) && x.ValidTo == null));
        Assert.False(await db.Set<WaferCompletion>().AnyAsync(x => x.WorkOrderId == Id(order)));
    }

    [Fact]
    public async Task Committed_receipt_cannot_bypass_revoked_fab_scope()
    {
        var order = await Order(1);
        var user = await f.NewUser("SystemAdmin");
        var key = Guid.NewGuid().ToString();
        var body = new
        {
            workOrderId = Id(order),
            lotNumber = Guid.NewGuid().ToString(),
            wafers = new[] { new { waferCode = Guid.NewGuid().ToString(), waferSizeMm = 300 } }
        };
        Assert.Equal(HttpStatusCode.Created, (await Send("production/lots", body, key: key, client: user.Client)).StatusCode);
        await using var db = f.Database.CreateContext();
        var scopes = await db.Set<UserFabScope>().Where(x => x.UserId == Id(user.User)).ToListAsync();
        db.RemoveRange(scopes);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await Send("production/lots", body, key: key, client: user.Client)).StatusCode);
    }

    [Fact]
    public async Task Production_identity_and_closed_membership_are_database_protected()
    {
        var lot = await Lot(await Order(1), 1);
        var waferId = lot.GetProperty("waferIds")[0].GetGuid();
        await using (var db = f.Database.CreateContext())
        {
            var wafer = await db.Set<Wafer>().FindAsync(waferId);
            wafer!.WorkOrderId = null;
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        await Ok($"production/lots/{Id(lot)}/cancel", new { reason = "验证历史保护" }, etag: E(lot));
        await using (var db = f.Database.CreateContext())
        {
            var member = await db.Set<LotMembership>().SingleAsync(x => x.WaferId == waferId);
            member.ValidTo = null;
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    // PostgreSQL identifiers cannot be parameters; the role is a fixed prefix plus a generated UUID.
#pragma warning disable EF1002
    [Fact]
    public async Task Runtime_role_can_create_release_and_load_without_ddl_or_history_delete_privilege()
    {
        var order = await Order(1);
        var user = (await Get("auth/me")).GetProperty("userId").GetGuid();
        var role = "mesagent_test_runtime_" + Guid.NewGuid().ToString("N");
        await using var db = f.Database.CreateContext();
        // Role name is generated here, never supplied by a client; no password or global grants are needed.
        await db.Database.ExecuteSqlRawAsync($"CREATE ROLE {role} NOLOGIN");
        try
        {
            await db.Database.ExecuteSqlRawAsync($"""
                GRANT USAGE ON SCHEMA organization,security,engineering,production,equipment,quality,materials,audit TO {role};
                GRANT SELECT ON ALL TABLES IN SCHEMA organization,security,engineering,production,equipment,quality,materials,audit TO {role};
                GRANT INSERT,UPDATE ON production.work_orders,production.lots,production.wafers,production.carriers,
                    production.carrier_loads,production.lot_memberships,production.step_executions TO {role};
                GRANT INSERT ON production.carrier_slots,production.carrier_movements,production.genealogy_events,
                    production.genealogy_details,production.wafer_releases,production.wafer_completions,
                    production.step_execution_inheritances,production.step_documents,audit.audit_logs,
                    audit.business_events,audit.command_receipts TO {role};
                """);
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                await db.Database.ExecuteSqlRawAsync($"SET LOCAL ROLE {role}");
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(78001001)");
                var backend = new ProductionBackend(db, f.Fab, new RequestActor(user, Guid.NewGuid(), Guid.NewGuid()),
                    p => Assert.Contains(p, PermissionCatalog.All), _ => true, TimeProvider.System);
                async Task<JsonElement> Execute(string path, object body, string? version = null)
                {
                    var result = await backend.Execute(new OrganizationIdentityAccessControlRequest("POST", path,
                        JsonSerializer.SerializeToElement(body), new Dictionary<string, string>(), version, Guid.NewGuid().ToString()));
                    await db.SaveChangesAsync();
                    return JsonSerializer.SerializeToElement(result.Data);
                }
                var lot = await Execute("production/lots", new
                {
                    workOrderId = Id(order), lotNumber = Guid.NewGuid().ToString(),
                    wafers = new[] { new { waferCode = Guid.NewGuid().ToString(), waferSizeMm = 300 } }
                });
                lot = await Execute($"production/lots/{Id(lot)}/release", new { }, E(lot));
                var carrier = await Execute("production/carriers", new
                {
                    carrierCode = Guid.NewGuid().ToString(), carrierType = "Test", slotCapacity = 1, allowedWaferSizeMm = 300
                });
                var wafer = await db.Set<Wafer>().SingleAsync(x => x.WorkOrderId == Id(order));
                var slot = await db.Set<CarrierSlot>().SingleAsync(x => x.CarrierId == Id(carrier));
                var waferTag = "\"" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(wafer.RowVersion.ToString())) + "\"";
                await Execute("production/carrier-load-commands", new
                {
                    carrierVersions = new[] { new { id = Id(carrier), etag = E(carrier) } },
                    waferVersions = new[] { new { id = wafer.Id, etag = waferTag } },
                    lotVersions = new[] { new { id = Id(lot), etag = E(lot) } },
                    moves = new[] { new { waferId = wafer.Id, fromSlotId = (Guid?)null, toSlotId = slot.Id } }, reason = "权限验证"
                });
                Assert.False(await db.Database.SqlQueryRaw<bool>(
                    "SELECT has_schema_privilege(current_user, 'production', 'CREATE') AS \"Value\"").SingleAsync());
                Assert.False(await db.Database.SqlQueryRaw<bool>(
                    "SELECT has_table_privilege(current_user, 'production.genealogy_events', 'DELETE') AS \"Value\"").SingleAsync());
                await tx.RollbackAsync();
            }
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync($"DROP OWNED BY {role}");
            await db.Database.ExecuteSqlRawAsync($"DROP ROLE {role}");
        }
    }

#pragma warning restore EF1002

}
