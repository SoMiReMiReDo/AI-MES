using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Application;
using MESagent.Infrastructure.DispatchAndProductionExecution;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MESagent.Api.Tests;

public sealed class ExecutionTests(OrganizationIdentityAccessControlFixture f)
    : IClassFixture<OrganizationIdentityAccessControlFixture>
{
    private static Guid Id(JsonElement row) => row.GetProperty("id").GetGuid();
    private static string E(JsonElement row) => row.GetProperty("etag").GetString()!;
    private Task<JsonElement> Get(string path) => f.Admin.GetFromJsonAsync<JsonElement>("/api/v1/" + path);

    private async Task<HttpResponseMessage> Send(string path, object body, string? version = null,
        string? key = null, HttpClient? client = null, string method = "POST")
    {
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/v1/" + path)
        {
            Content = JsonContent.Create(body)
        };
        if (version != null)
            request.Headers.TryAddWithoutValidation("If-Match", version);
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString());
        return await (client ?? f.Admin).SendAsync(request);
    }

    private async Task<JsonElement> Ok(string path, object body, string? version = null, string method = "POST")
    {
        var response = await Send(path, body, version, method: method);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private sealed record Sample(JsonElement Order, JsonElement Lot, JsonElement Tool, JsonElement Recipe,
        JsonElement[] Steps, JsonElement? Checklist, JsonElement? Output);

    private async Task<Sample> Setup(int count = 5, int stations = 3, bool work = false)
    {
        var code = Guid.NewGuid().ToString("N");
        var area = await Ok("organization/areas", new { code, name = "执行测试区域", fabId = f.Fab });
        var group = await Ok("equipment/groups", new { code, name = "演示设备组" });
        var type = await Ok("equipment/types", new { code, name = "演示类型" });
        var tool = await Ok("equipment/tools", new
        {
            code,
            name = "演示设备",
            areaId = Id(area),
            equipmentGroupId = Id(group),
            equipmentTypeId = Id(type)
        });
        var product = await Ok("engineering/products", new { code, name = "执行产品", unitOfMeasure = "wafer" });
        var route = await Ok("engineering/routes", new { code, name = "执行路线", productId = Id(product) });
        var version = await Ok("engineering/route-versions", new { routeId = Id(route), version = "1" });
        var operation = await Ok("engineering/operations", new { code, name = "执行工序", segmentType = "Process" });
        var recipe = await Ok("engineering/recipes", new { code, name = "执行配方" });
        var recipeVersion = await Ok("engineering/recipe-versions", new { recipeId = Id(recipe), version = "1" });
        recipeVersion = await Ok($"engineering/recipe-versions/{Id(recipeVersion)}/release",
            new { reason = "发布" }, E(recipeVersion));
        var steps = new List<JsonElement>();
        JsonElement? checklist = null;
        JsonElement? output = null;
        for (var i = 1; i <= stations; i++)
        {
            var step = await Ok("engineering/route-steps", new
            {
                routeVersionId = Id(version),
                operationId = Id(operation),
                sequenceNo = i * 10,
                name = "执行站 " + i,
                equipmentTypeId = Id(type),
                requiresConfirmation = work,
                instruction = "核对晶圆与配方后加工"
            });
            steps.Add(step);
            await Ok("engineering/recipe-applicabilities", new
            {
                recipeVersionId = Id(recipeVersion),
                routeStepId = Id(step),
                equipmentId = Id(tool),
                effectiveFrom = DateTime.UtcNow.AddDays(-1)
            });
            if (work && i == 1)
            {
                checklist = await Ok("engineering/checklist-items", new
                {
                    routeStepId = Id(step),
                    label = "核对载具",
                    isRequired = true
                });
                output = await Ok("engineering/output-definitions", new
                {
                    routeStepId = Id(step),
                    key = "temperature",
                    label = "温度",
                    valueType = "decimal",
                    unit = "C",
                    isRequired = true
                });
                var doc = await Ok("engineering/documents", new
                {
                    entityType = "RouteStep",
                    entityId = Id(step),
                    revision = "A",
                    filename = "instruction.pdf",
                    content = Convert.ToBase64String("%PDF-1.7\n%%EOF"u8.ToArray())
                });
                await Ok($"engineering/documents/{Id(doc)}/release", new { }, E(doc));
            }
        }
        await Ok($"engineering/route-versions/{Id(version)}/release", new { reason = "发布" }, E(version));
        var revision = await Ok("engineering/product-revisions", new
        {
            productId = Id(product),
            routeVersionId = Id(version),
            revisionCode = "A"
        });
        await Ok($"engineering/product-revisions/{Id(revision)}/release", new { reason = "发布" }, E(revision));
        var order = await Ok("planning/work-orders", new
        {
            orderNo = code,
            productId = Id(product),
            productRevisionId = Id(revision),
            plannedQty = count
        });
        order = await Ok($"planning/work-orders/{Id(order)}/release", new { reason = "下达" }, E(order));
        var lot = await Ok("production/lots", new
        {
            workOrderId = Id(order),
            lotNumber = code,
            wafers = Enumerable.Range(1, count).Select(_ => new
            {
                waferCode = Guid.NewGuid().ToString("N"),
                waferSizeMm = 300
            })
        });
        lot = await Ok($"production/lots/{Id(lot)}/release", new { }, E(lot));
        return new(order, lot, tool, recipeVersion, steps.ToArray(), checklist, output);
    }

    private async Task<JsonElement> Start(Sample sample, JsonElement? lot = null)
    {
        var current = lot ?? (await Get($"execution/lots/{Id(sample.Lot)}")).GetProperty("lot");
        var tool = (await Get($"equipment/tools/{Id(sample.Tool)}")).GetProperty("equipment");
        return await Ok($"execution/lots/{Id(current)}/track-in", new
        {
            equipmentId = Id(tool),
            equipmentEtag = E(tool),
            recipeVersionId = Id(sample.Recipe)
        }, E(current));
    }

    private async Task<JsonElement> End(JsonElement run, bool failed = false)
    {
        var detail = await Get($"execution/runs/{Id(run)}");
        return await Ok($"execution/runs/{Id(run)}/track-out", new
        {
            wafers = detail.GetProperty("wafers").EnumerateArray().Select((x, i) => new
            {
                waferId = Id(x),
                result = failed && i == 0 ? "Failed" : "Passed"
            })
        }, E(run));
    }

    [Fact]
    public async Task Five_wafers_three_stations_complete_through_http_with_real_progress_and_close()
    {
        var sample = await Setup();
        var dispatch = await Get($"execution/dispatch?lotNumber={sample.Lot.GetProperty("lotNumber").GetString()}");
        Assert.True(dispatch.GetProperty("items")[0].GetProperty("executable").GetBoolean());
        for (var i = 0; i < 3; i++)
            await End(await Start(sample));
        var progress = await Get($"planning/work-orders/{Id(sample.Order)}/progress");
        Assert.Equal("ProductionFacts", progress.GetProperty("coverage").GetString());
        Assert.Equal(5, progress.GetProperty("releasedQty").GetInt32());
        Assert.Equal(5, progress.GetProperty("completedQty").GetInt32());
        Assert.Equal(0, progress.GetProperty("inProcessQty").GetInt32());
        var order = await Get($"planning/work-orders/{Id(sample.Order)}");
        await Ok($"planning/work-orders/{Id(order)}/close", new { reason = "完工关闭" }, E(order));
        await using var db = f.Database.CreateContext();
        var stepIds = db.Set<StepExecution>().Where(x => x.LotId == Id(sample.Lot)).Select(x => x.Id);
        var runIds = db.Set<ProcessRun>().Where(x => stepIds.Contains(x.StepExecutionId)).Select(x => x.Id);
        Assert.Equal(15, await db.Set<ProcessRunWafer>().CountAsync(x => runIds.Contains(x.ProcessRunId)));
        Assert.Equal(6, await db.Set<TrackEvent>().CountAsync(x => runIds.Contains(x.ProcessRunId)));
        Assert.Equal(5, await db.Set<WaferCompletion>().CountAsync(x => x.WorkOrderId == Id(order)));
        Assert.Equal("Completed", (await db.Set<Lot>().FindAsync(Id(sample.Lot)))!.Status);
    }

    [Fact]
    public async Task Required_work_types_units_and_bound_pdf_are_enforced_atomically()
    {
        var sample = await Setup(1, 1, true);
        var run = await Start(sample);
        var detail = await Get($"execution/runs/{Id(run)}");
        var waferId = Id(detail.GetProperty("wafers")[0]);
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"execution/runs/{Id(run)}/track-out",
            new { wafers = new[] { new { waferId, result = "Passed" } } }, E(run))).StatusCode);
        var docId = Id(detail.GetProperty("work").GetProperty("documents")[0]);
        Assert.Equal(HttpStatusCode.OK, (await f.Admin.GetAsync(
            $"/api/v1/execution/lots/{Id(sample.Lot)}/documents/{docId}/content")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync(
            $"/api/v1/execution/lots/{Id(sample.Lot)}/documents/{Guid.NewGuid()}/content")).StatusCode);
        var invalid = await Send($"execution/runs/{Id(run)}/records", new
        {
            checklistItemIds = new[] { Id(sample.Checklist!.Value) },
            documentIds = new[] { docId },
            confirm = true,
            outputs = new[] { new { outputId = Id(sample.Output!.Value), unit = "wrong", valueNumber = 10 } }
        }, E(run));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        await using (var db = f.Database.CreateContext())
            Assert.False(await db.Set<ChecklistCompletion>().AnyAsync(x => x.StepExecutionId ==
                detail.GetProperty("step").GetProperty("id").GetGuid()));
        run = await Ok($"execution/runs/{Id(run)}/records", new
        {
            checklistItemIds = new[] { Id(sample.Checklist!.Value) },
            documentIds = new[] { docId },
            confirm = true,
            outputs = new[] { new { outputId = Id(sample.Output!.Value), unit = "C", valueNumber = 10.123456m } }
        }, E(run));
        await End(run);
    }

    [Fact]
    public async Task Failed_wafer_is_not_scrap_and_does_not_advance()
    {
        var sample = await Setup(2, 2);
        var run = await Start(sample);
        var ended = await End(run, true);
        Assert.Equal("AwaitingDisposition", ended.GetProperty("lot").GetProperty("status").GetString());
        Assert.False(ended.GetProperty("advanced").GetBoolean());
        var progress = await Get($"planning/work-orders/{Id(sample.Order)}/progress");
        Assert.Equal(0, progress.GetProperty("completedQty").GetInt32());
        Assert.Equal(0, progress.GetProperty("scrappedQty").GetInt32());
        Assert.Equal(2, progress.GetProperty("blockedQty").GetInt32());
        var tool = (await Get($"equipment/tools/{Id(sample.Tool)}")).GetProperty("equipment");
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"execution/lots/{Id(sample.Lot)}/track-in", new
        {
            equipmentId = Id(tool),
            equipmentEtag = E(tool),
            recipeVersionId = Id(sample.Recipe)
        }, E(ended.GetProperty("lot")))).StatusCode);
    }

    [Fact]
    public async Task Abort_records_reason_releases_occupancy_and_requires_permission()
    {
        var sample = await Setup(1, 1);
        var run = await Start(sample);
        var viewer = await f.NewUser();
        Assert.Equal(HttpStatusCode.Forbidden, (await Send($"execution/runs/{Id(run)}/abort",
            new { reason = "异常" }, E(run), client: viewer.Client)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send($"execution/runs/{Id(run)}/abort",
            new { reason = " " }, E(run))).StatusCode);
        var result = await Ok($"execution/runs/{Id(run)}/abort", new { reason = "设备异常" }, E(run));
        Assert.Equal("Aborted", result.GetProperty("run").GetProperty("status").GetString());
        var tool = await Get($"equipment/tools/{Id(sample.Tool)}");
        Assert.Equal(JsonValueKind.Null, tool.GetProperty("currentRun").ValueKind);
        var detail = await Get($"execution/runs/{Id(run)}");
        Assert.Equal("Aborted", detail.GetProperty("wafers")[0].GetProperty("result").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"execution/runs/{Id(run)}/abort",
            new { reason = "重复结束" }, E(result.GetProperty("run")))).StatusCode);
    }

    [Fact]
    public async Task Track_in_replay_and_device_concurrency_have_one_result()
    {
        var sample = await Setup(2, 1);
        var key = Guid.NewGuid().ToString();
        var body = new
        {
            equipmentId = Id(sample.Tool),
            equipmentEtag = E(sample.Tool),
            recipeVersionId = Id(sample.Recipe)
        };
        var path = $"execution/lots/{Id(sample.Lot)}/track-in";
        var responses = await Task.WhenAll(Send(path, body, E(sample.Lot), key), Send(path, body, E(sample.Lot), key));
        Assert.All(responses, x => Assert.Equal(HttpStatusCode.Created, x.StatusCode));
        Assert.Contains(responses, x => x.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(Id(await responses[0].Content.ReadFromJsonAsync<JsonElement>()),
            Id(await responses[1].Content.ReadFromJsonAsync<JsonElement>()));
        Assert.Equal(HttpStatusCode.Conflict,
            (await Send(path, new { equipmentId = Guid.NewGuid() }, E(sample.Lot), key)).StatusCode);
    }

    [Fact]
    public async Task Split_lots_compete_for_same_equipment_and_complete_without_double_counting()
    {
        var sample = await Setup();
        await End(await Start(sample));
        var lot = (await Get($"execution/lots/{Id(sample.Lot)}")).GetProperty("lot");
        var members = await Get($"production/lots/{Id(lot)}/wafers");
        var split = await Ok($"production/lots/{Id(lot)}/split", new
        {
            waferIds = members.GetProperty("items").EnumerateArray().Take(2).Select(Id),
            reason = "2+3",
            newLotNumber = Guid.NewGuid().ToString("N")
        }, E(lot));
        var child = split.GetProperty("newLot");
        lot = (await Get($"execution/lots/{Id(lot)}")).GetProperty("lot");
        var tool = (await Get($"equipment/tools/{Id(sample.Tool)}")).GetProperty("equipment");
        var body = new { equipmentId = Id(tool), equipmentEtag = E(tool), recipeVersionId = Id(sample.Recipe) };
        var responses = await Task.WhenAll(Send($"execution/lots/{Id(lot)}/track-in", body, E(lot)),
            Send($"execution/lots/{Id(child)}/track-in", body, E(child)));
        Assert.Single(responses, x => x.IsSuccessStatusCode);
        Assert.Single(responses, x => x.StatusCode is HttpStatusCode.PreconditionFailed or HttpStatusCode.Conflict);
        var winning = await responses.Single(x => x.IsSuccessStatusCode).Content.ReadFromJsonAsync<JsonElement>();
        await End(winning);
        var winner = (await Get($"execution/runs/{Id(winning)}")).GetProperty("lot");
        var loser = Id(winner) == Id(lot) ? child : lot;
        await End(await Start(sample, loser));
        var sources = new List<object>();
        foreach (var id in new[] { Id(lot), Id(child) })
        {
            var current = (await Get($"execution/lots/{id}")).GetProperty("lot");
            var wafers = (await Get($"production/lots/{id}/wafers")).GetProperty("items");
            sources.Add(new { lotId = id, etag = E(current), waferIds = wafers.EnumerateArray().Select(Id).ToArray() });
        }
        var merged = await Ok("production/lot-merges", new
        {
            sources,
            reason = "末站前合回",
            newLotNumber = Guid.NewGuid().ToString("N")
        });
        await End(await Start(sample, merged.GetProperty("newLot")));
        var progress = await Get($"planning/work-orders/{Id(sample.Order)}/progress");
        Assert.Equal(5, progress.GetProperty("releasedQty").GetInt32());
        Assert.Equal(5, progress.GetProperty("completedQty").GetInt32());
    }

    [Fact]
    public async Task Hold_during_processing_allows_close_without_advance()
    {
        var sample = await Setup(1, 2);
        var run = await Start(sample);
        await using (var db = f.Database.CreateContext())
        {
            var user = (await Get("auth/me")).GetProperty("userId").GetGuid();
            var reason = new ReasonCode { Code = Guid.NewGuid().ToString(), Name = "测试 Hold", Category = "Hold" };
            db.Add(reason);
            db.Add(new HoldRecord
            {
                LotId = Id(sample.Lot),
                ReasonCodeId = reason.Id,
                HeldById = user,
                HeldAt = DateTime.UtcNow,
                HoldScope = "Lot"
            });
            await db.SaveChangesAsync();
        }
        var result = await End(run);
        Assert.Equal("AwaitingDisposition", result.GetProperty("lot").GetProperty("status").GetString());
        Assert.False(result.GetProperty("advanced").GetBoolean());
    }

    [Fact]
    public async Task Equipment_state_version_and_unknown_recipe_block_start()
    {
        var sample = await Setup(1, 1);
        var path = $"execution/lots/{Id(sample.Lot)}/track-in";
        var body = new
        {
            equipmentId = Id(sample.Tool),
            equipmentEtag = E(sample.Tool),
            recipeVersionId = Id(sample.Recipe)
        };
        Assert.Equal(HttpStatusCode.PreconditionRequired, (await Send(path, body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(path, new
        {
            equipmentId = Id(sample.Tool),
            equipmentEtag = E(sample.Tool),
            recipeVersionId = Guid.NewGuid()
        }, E(sample.Lot))).StatusCode);
        var down = await Ok($"equipment/tools/{Id(sample.Tool)}/state",
            new { state = "Down", reason = "故障" }, E(sample.Tool));
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Send(path, body, E(sample.Lot))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(path, new
        {
            equipmentId = Id(down),
            equipmentEtag = E(down),
            recipeVersionId = Id(sample.Recipe)
        }, E(sample.Lot))).StatusCode);
    }

    [Fact]
    public async Task Invalid_wafer_set_does_not_close_run_and_finished_history_is_protected()
    {
        var sample = await Setup(2, 1);
        var run = await Start(sample);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send($"execution/runs/{Id(run)}/track-out",
            new { wafers = new[] { new { waferId = Guid.NewGuid(), result = "Passed" } } }, E(run))).StatusCode);
        await End(run);
        await using var db = f.Database.CreateContext();
        var finished = await db.Set<ProcessRun>().FindAsync(Id(run));
        finished!.EquipmentId = Guid.NewGuid();
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Theory]
    [InlineData("decimal", "C", "wrong", false)]
    [InlineData("boolean", null, null, true)]
    [InlineData("integer", null, null, true)]
    public void Output_values_follow_existing_engineering_types(
        string type, string? unit, string? supplied, bool expected)
    {
        var valid = type == "boolean"
            ? ExecutionRules.ValidOutput(type, null, null, null, false, null)
            : ExecutionRules.ValidOutput(type, null, null, 1, null, null);
        Assert.Equal(expected, valid && unit == supplied);
        Assert.False(ExecutionRules.ValidOutput("decimal", null, null, 1.1234567m, null, null));
        Assert.False(ExecutionRules.ValidOutput("enum", "[\"a\"]", "b", null, null, null));
    }

    [Fact]
    public async Task Two_holds_and_unimplemented_qtime_are_independent_start_gates()
    {
        var sample = await Setup(1, 2);
        var user = (await Get("auth/me")).GetProperty("userId").GetGuid();
        Guid first;
        Guid second;
        await using (var db = f.Database.CreateContext())
        {
            var reason = new ReasonCode { Code = Guid.NewGuid().ToString(), Name = "冻结", Category = "Hold" };
            db.Add(reason);
            HoldRecord Hold() => new()
            {
                LotId = Id(sample.Lot),
                ReasonCodeId = reason.Id,
                HoldScope = "Lot",
                HeldAt = DateTime.UtcNow,
                HeldById = user
            };
            var a = Hold();
            var b = Hold();
            first = a.Id;
            second = b.Id;
            db.AddRange(a, b);
            await db.SaveChangesAsync();
        }
        var body = new
        {
            equipmentId = Id(sample.Tool),
            equipmentEtag = E(sample.Tool),
            recipeVersionId = Id(sample.Recipe)
        };
        var path = $"execution/lots/{Id(sample.Lot)}/track-in";
        Assert.Equal(HttpStatusCode.Conflict, (await Send(path, body, E(sample.Lot))).StatusCode);
        await using (var db = f.Database.CreateContext())
        {
            var hold = await db.Set<HoldRecord>().FindAsync(first);
            hold!.ReleasedAt = DateTime.UtcNow;
            hold.ReleasedById = user;
            hold.ReleaseReason = "隔离测试解除第一条 Hold";
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Conflict, (await Send(path, body, E(sample.Lot))).StatusCode);
        await using (var db = f.Database.CreateContext())
        {
            var hold = await db.Set<HoldRecord>().FindAsync(second);
            hold!.ReleasedAt = DateTime.UtcNow;
            hold.ReleasedById = user;
            hold.ReleaseReason = "隔离测试解除第二条 Hold";
            db.Add(new QTimeRule
            {
                StartRouteStepId = Id(sample.Steps[0]),
                EndRouteStepId = Id(sample.Steps[1]),
                StartEventType = "TrackOut",
                EndEventType = "TrackIn",
                LimitMinutes = 30,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
        var response = await Send(path, body, E(sample.Lot));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("DependencyNotReady", (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("code").GetString());
    }

    [Fact]
    public async Task Recalculation_uses_facts_and_active_work_prevents_close()
    {
        var sample = await Setup(1, 1);
        var order = await Get($"planning/work-orders/{Id(sample.Order)}");
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"planning/work-orders/{Id(order)}/close",
            new { reason = "仍在制" }, E(order))).StatusCode);
        await End(await Start(sample));
        await using (var db = f.Database.CreateContext())
        {
            var cached = await db.Set<WorkOrder>().FindAsync(Id(order));
            cached!.ProducedQty = 0;
            cached.CompletedAt = null;
            await db.SaveChangesAsync();
        }
        Assert.Equal(1, (await Get($"planning/work-orders/{Id(order)}/progress"))
            .GetProperty("completedQty").GetInt32());
        order = await Get($"planning/work-orders/{Id(order)}");
        var result = await Ok($"planning/work-orders/{Id(order)}/recalculate", new { reason = "验证缓存恢复" }, E(order));
        Assert.Equal(1, result.GetProperty("producedQty").GetInt32());
        await using var verify = f.Database.CreateContext();
        Assert.Single(await verify.Set<WaferCompletion>().Where(x => x.WorkOrderId == Id(order)).ToListAsync());
    }

    [Fact]
    public async Task Revoked_scope_blocks_successful_receipt_and_foreign_objects_are_invisible()
    {
        var sample = await Setup(1, 1);
        var user = await f.NewUser("SystemAdmin");
        var key = Guid.NewGuid().ToString();
        var path = $"execution/lots/{Id(sample.Lot)}/track-in";
        var body = new
        {
            equipmentId = Id(sample.Tool),
            equipmentEtag = E(sample.Tool),
            recipeVersionId = Id(sample.Recipe)
        };
        Assert.Equal(HttpStatusCode.Created, (await Send(path, body, E(sample.Lot), key, user.Client)).StatusCode);
        await using (var db = f.Database.CreateContext())
        {
            db.RemoveRange(await db.Set<UserFabScope>().Where(x => x.UserId == Id(user.User)).ToListAsync());
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(path, body, E(sample.Lot), key, user.Client)).StatusCode);
        var foreign = await f.Database.SeedAsync();
        Assert.Equal(HttpStatusCode.NotFound,
            (await f.Admin.GetAsync($"/api/v1/execution/lots/{foreign.Lot}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await f.Admin.GetAsync($"/api/v1/equipment/tools/{foreign.Equipment}")).StatusCode);
    }

    [Fact]
    public async Task Query_and_wrong_http_method_never_start_processing()
    {
        var sample = await Setup(1, 1);
        var path = $"execution/lots/{Id(sample.Lot)}/track-in";
        Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync("/api/v1/" + path)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(path, new
        {
            equipmentId = Id(sample.Tool),
            equipmentEtag = E(sample.Tool),
            recipeVersionId = Id(sample.Recipe)
        }, E(sample.Lot), method: "PUT")).StatusCode);
        await using var db = f.Database.CreateContext();
        Assert.False(await db.Set<ProcessRun>().AnyAsync(x => db.Set<StepExecution>()
            .Any(s => s.Id == x.StepExecutionId && s.LotId == Id(sample.Lot))));
    }

    [Fact]
    public async Task Actual_recipe_match_is_preserved_when_future_applicability_is_revoked()
    {
        var sample = await Setup(1, 1);
        var run = await Start(sample);
        await using (var db = f.Database.CreateContext())
        {
            var application = await db.Set<RecipeApplicability>().SingleAsync(x => x.EquipmentId == Id(sample.Tool));
            application.EffectiveTo = DateTime.UtcNow.AddHours(-1);
            await db.SaveChangesAsync();
        }
        await Ok($"engineering/recipe-versions/{Id(sample.Recipe)}/obsolete", new { reason = "停止后续采用" },
            E(sample.Recipe));
        await End(run);
        var detail = await Get($"execution/runs/{Id(run)}");
        Assert.Equal(Id(sample.Recipe), detail.GetProperty("recipe").GetProperty("id").GetGuid());
        var track = detail.GetProperty("tracks").EnumerateArray()
            .Single(x => x.GetProperty("eventType").GetString() == "TrackIn");
        using var payload = JsonDocument.Parse(track.GetProperty("payload").GetString()!);
        var match = payload.RootElement.GetProperty("match");
        Assert.Equal(JsonValueKind.Null, match.GetProperty("effectiveTo").ValueKind);
        Assert.False(string.IsNullOrEmpty(match.GetProperty("applicabilityEtag").GetString()));
        Assert.Equal(Id(sample.Recipe), match.GetProperty("recipeVersionId").GetGuid());
    }

#pragma warning disable EF1002
    [Fact]
    public async Task Runtime_role_can_execute_terminal_run_without_ddl_or_history_delete()
    {
        var sample = await Setup(1, 1);
        var user = (await Get("auth/me")).GetProperty("userId").GetGuid();
        var role = "mesagent_test_execution_" + Guid.NewGuid().ToString("N");
        await using var db = f.Database.CreateContext();
        await db.Database.ExecuteSqlRawAsync($"CREATE ROLE {role} NOLOGIN");
        try
        {
            await db.Database.ExecuteSqlRawAsync($"""
                GRANT USAGE ON SCHEMA organization,security,engineering,production,equipment,quality,audit TO {role};
                GRANT SELECT ON ALL TABLES IN SCHEMA organization,security,engineering,production,equipment,quality,audit TO {role};
                GRANT UPDATE ON production.lots,production.wafers,production.step_executions,production.work_orders,
                    production.carriers,equipment.equipment TO {role};
                GRANT INSERT,UPDATE ON production.process_runs,production.process_run_wafers,production.step_documents TO {role};
                GRANT INSERT ON production.wafer_completions,production.track_events,
                    audit.audit_logs,audit.business_events,audit.command_receipts TO {role};
                """);
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                await db.Database.ExecuteSqlRawAsync($"SET LOCAL ROLE {role}");
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(78001001)");
                var backend = new ExecutionBackend(db, f.Fab, new RequestActor(user, Guid.NewGuid(), Guid.NewGuid()),
                    p => Assert.Contains(p, PermissionCatalog.All), _ => true, TimeProvider.System);
                async Task<JsonElement> Execute(string path, object body, string version)
                {
                    var request = new OrganizationIdentityAccessControlRequest("POST", path,
                        JsonSerializer.SerializeToElement(body), new Dictionary<string, string>(), version,
                        Guid.NewGuid().ToString());
                    var result = await backend.Execute(request);
                    await db.SaveChangesAsync();
                    return JsonSerializer.SerializeToElement(result.Data);
                }
                var run = await Execute($"execution/lots/{Id(sample.Lot)}/track-in", new
                {
                    equipmentId = Id(sample.Tool),
                    equipmentEtag = E(sample.Tool),
                    recipeVersionId = Id(sample.Recipe)
                }, E(sample.Lot));
                var ids = await db.Set<ProcessRunWafer>().Where(x => x.ProcessRunId == Id(run))
                    .Select(x => x.WaferId).ToArrayAsync();
                var ended = await Execute($"execution/runs/{Id(run)}/track-out", new
                {
                    wafers = ids.Select(waferId => new { waferId, result = "Passed" })
                }, E(run));
                Assert.Equal("Completed", ended.GetProperty("lot").GetProperty("status").GetString());
                Assert.False(await db.Database.SqlQueryRaw<bool>(
                    "SELECT has_schema_privilege(current_user,'production','CREATE') AS \"Value\"").SingleAsync());
                Assert.False(await db.Database.SqlQueryRaw<bool>(
                    "SELECT has_table_privilege(current_user,'production.process_runs','DELETE') AS \"Value\"").SingleAsync());
                await tx.CommitAsync();
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
