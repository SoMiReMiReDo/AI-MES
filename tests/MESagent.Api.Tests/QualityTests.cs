using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Application;
using MESagent.Infrastructure.QualityExceptionDisposition;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MESagent.Api.Tests;

public sealed class QualityTests(OrganizationIdentityAccessControlFixture f)
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

    private async Task<JsonElement> QLot(Sample s) => await Get($"quality/lots/{Id(s.Lot)}");

    private async Task<JsonElement> Reason(string category) => await Ok("quality/reason-codes", new
    {
        code = Guid.NewGuid().ToString("N"),
        name = category + "原因",
        category
    });

    private async Task<JsonElement> Hold(Sample sample, JsonElement reason)
    {
        var lot = await QLot(sample);
        await Ok($"quality/lots/{Id(lot)}/holds", new { reasonCodeId = Id(reason) }, E(lot));
        return (await QLot(sample)).GetProperty("holds").EnumerateArray()
            .Single(x => x.GetProperty("reasonCodeId").GetGuid() == Id(reason));
    }

    private async Task ReleaseHold(JsonElement hold) => await Ok($"quality/holds/{Id(hold)}/release",
        new { reason = "已完成审核" }, E(hold));

    private async Task<JsonElement> Release(Sample sample, string action)
    {
        var lot = await QLot(sample);
        return await Ok($"quality/lots/{Id(lot)}/release", new { action, reason = "质量审核允许" }, E(lot));
    }

    private async Task<JsonElement> RecordWork(Sample sample, JsonElement run)
    {
        var detail = await Get($"execution/runs/{Id(run)}");
        return await Ok($"execution/runs/{Id(run)}/records", new
        {
            checklistItemIds = new[] { Id(sample.Checklist!.Value) },
            documentIds = detail.GetProperty("work").GetProperty("documents").EnumerateArray().Select(Id),
            confirm = true,
            outputs = new[] { new { outputId = Id(sample.Output!.Value), unit = "C", valueNumber = 10 } }
        }, E(run));
    }

    [Fact]
    public async Task Issue_actions_holds_and_quality_release_have_independent_lifecycles()
    {
        var sample = await Setup(1, 1);
        var reason = await Reason("Hold");
        var type = await Ok("quality/issue-types", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "生产异常",
            severity = "Major",
            isBlocking = true
        });
        var issue = await Ok("quality/issues", new
        {
            issueTypeId = Id(type),
            title = "异常测试",
            description = "确认质量问题",
            lotId = Id(sample.Lot),
            holdReasonCodeId = Id(reason)
        });
        Assert.Equal("Open", issue.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"quality/issues/{Id(issue)}/close",
            new { reason = "不能直接关闭" }, E(issue))).StatusCode);
        issue = await Ok($"quality/issues/{Id(issue)}/acknowledge", new { reason = "确认" }, E(issue));
        var me = await Get("auth/me");
        issue = await Ok($"quality/issues/{Id(issue)}/assign",
            new { assignedToId = me.GetProperty("userId").GetGuid() },
            E(issue));
        var action = await Ok($"quality/issues/{Id(issue)}/actions", new
        {
            title = "纠正措施",
            actionType = "Corrective",
            assignedToId = me.GetProperty("userId").GetGuid()
        }, E(issue));
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"quality/actions/{Id(action)}/verify",
            new { reason = "未完成不能复核" }, E(action))).StatusCode);
        action = await Ok($"quality/actions/{Id(action)}/complete", new { reason = "已完成" }, E(action));
        await Ok($"quality/actions/{Id(action)}/verify", new { reason = "已复核" }, E(action));
        issue = await Get($"quality/issues/{Id(issue)}");
        issue = await Ok($"quality/issues/{Id(issue)}/resolve", new
        {
            disposition = "Continue",
            rootCause = "原因",
            containmentAction = "已隔离",
            notes = "审核"
        }, E(issue));
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"quality/issues/{Id(issue)}/close",
            new { reason = "Hold 仍在" }, E(issue))).StatusCode);
        var detail = await Get($"quality/issues/{Id(issue)}");
        await ReleaseHold(detail.GetProperty("holds")[0]);
        await Ok($"quality/issues/{Id(issue)}/close", new { reason = "关闭" }, E(issue));
        var dispatch = await Get($"execution/lots/{Id(sample.Lot)}");
        Assert.Contains(dispatch.GetProperty("blocks").EnumerateArray(), x => x.GetString()!.Contains("质量放行"));
        await Release(sample, "None");
        await End(await Start(sample));
        Assert.Equal("Completed", (await QLot(sample)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Multiple_holds_on_running_lot_require_separate_release_then_continue()
    {
        var sample = await Setup(2, 2);
        var run = await Start(sample);
        var a = await Hold(sample, await Reason("Hold"));
        var b = await Hold(sample, await Reason("Hold"));
        await End(run);
        await ReleaseHold(a);
        var lot = await QLot(sample);
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"quality/lots/{Id(lot)}/release",
            new { action = "Continue", reason = "仍有 Hold" }, E(lot))).StatusCode);
        await ReleaseHold(b);
        await Release(sample, "Continue");
        lot = await QLot(sample);
        Assert.Equal("Ready", lot.GetProperty("status").GetString());
        Assert.Equal(Id(sample.Steps[1]), lot.GetProperty("currentRouteStepId").GetGuid());
        await End(await Start(sample));
        var progress = await Get($"planning/work-orders/{Id(sample.Order)}/progress");
        Assert.Equal(2, progress.GetProperty("completedQty").GetInt32());
        Assert.Equal(0, progress.GetProperty("blockedQty").GetInt32());
    }

    [Fact]
    public async Task Terminal_quality_continue_creates_completion_once_and_preserves_original_actor()
    {
        var sample = await Setup(2, 1);
        var run = await Start(sample);
        var hold = await Hold(sample, await Reason("Hold"));
        await End(run);
        await ReleaseHold(hold);
        var lot = await QLot(sample);
        var key = Guid.NewGuid().ToString();
        var body = new { action = "Continue", reason = "终点质量审核" };
        var path = $"quality/lots/{Id(lot)}/release";
        var response = await Send(path, body, E(lot), key);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var replay = await Send(path, body, E(lot), key);
        Assert.True(replay.IsSuccessStatusCode);
        Assert.True(replay.Headers.Contains("Idempotency-Replayed"));
        var view = await Get($"production/lots/{Id(lot)}");
        Assert.Equal(2, view.GetProperty("quantities").GetProperty("completedQty").GetInt32());
        Assert.Equal("ProductionFacts", view.GetProperty("coverage").GetString());
        await using var db = f.Database.CreateContext();
        Assert.Equal(2, await db.Set<WaferCompletion>().CountAsync(x => x.WorkOrderId == Id(sample.Order)));
        Assert.Equal("Completed", (await db.Set<ProcessRun>().FindAsync(Id(run)))!.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_or_aborted_run_requires_new_visit_and_fresh_required_work(bool abort)
    {
        var sample = await Setup(1, 1, true);
        var run = await RecordWork(sample, await Start(sample));
        if (abort)
            await Ok($"execution/runs/{Id(run)}/abort", new { reason = "中止测试" }, E(run));
        else
            await End(run, true);
        var lot = await QLot(sample);
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"quality/lots/{Id(lot)}/release",
            new { action = "Continue", reason = "禁止跳过失败" }, E(lot))).StatusCode);
        await Release(sample, "Retry");
        lot = await QLot(sample);
        Assert.Equal(2, lot.GetProperty("step").GetProperty("visitNo").GetInt32());
        var next = await Start(sample);
        var detail = await Get($"execution/runs/{Id(next)}");
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"execution/runs/{Id(next)}/track-out", new
        {
            wafers = detail.GetProperty("wafers").EnumerateArray().Select(w => new
            {
                waferId = Id(w),
                result = "Passed"
            })
        }, E(next))).StatusCode);
        await End(await RecordWork(sample, next));
        var original = await Get($"execution/runs/{Id(run)}");
        Assert.Equal(abort ? "Aborted" : "Failed", original.GetProperty("wafers")[0].GetProperty("result").GetString());
        Assert.Equal("Completed", (await QLot(sample)).GetProperty("status").GetString());
        var waferId = Id(original.GetProperty("wafers")[0]);
        var timeline = await f.Admin.GetFromJsonAsync<PageView<TraceEvent>>(
            $"/api/v1/traceability/wafer/{waferId}/events?group=execution");
        var visits = timeline!.Items.Where(x => x.Type == "step-visit").ToArray();
        Assert.Equal(2, visits.Length);
        Assert.NotEqual(visits[0].SourceId, visits[1].SourceId);
        Assert.Contains(timeline.Items, x => x.Type == "run-ended" && x.SourceId == Id(run)
            && x.Result == (abort ? "Aborted" : "Completed"));
        Assert.Contains(timeline.Items, x => x.Type == "run-wafer" && x.Result == (abort ? "Aborted" : "Failed"));
        Assert.Contains(timeline.Items, x => x.Type == "run-ended" && x.SourceId == Id(next)
            && x.Result == "Completed");
    }
    private async Task<(JsonElement Template, JsonElement[] Specs)> InspectionSetup(Sample sample,
        string? templateName = null)
    {
        var definitions = new object[]
        {
            new { parameterCode = Guid.NewGuid().ToString("N"), version = 1,
                valueType = "decimal", unit = "nm", specMin = 10, specMax = 20 },

            new { parameterCode = Guid.NewGuid().ToString("N"), version = 1,
                valueType = "boolean", expectedBoolean = true },
            new { parameterCode = Guid.NewGuid().ToString("N"), version = 1, valueType = "string", expectedText = "OK" }
        };
        var specs = new List<JsonElement>();
        foreach (var definition in definitions)
        {
            var spec = await Ok("quality/specifications", definition);
            specs.Add(await Ok($"quality/specifications/{Id(spec)}/release", new { reason = "发布规格" }, E(spec)));
        }
        var lot = await QLot(sample);
        var template = await Ok("quality/templates", new
        {
            routeVersionId = lot.GetProperty("routeVersionId").GetGuid(),
            name = templateName ?? "模板 " + Guid.NewGuid(),
            version = 1,
            samplesPerCheck = 1,
            specificationVersionIds = specs.Select(Id)
        });
        template = await Ok($"quality/templates/{Id(template)}/release", new { reason = "发布模板" }, E(template));
        return (template, specs.ToArray());
    }

    private static object[] MeasurementRows(JsonElement[] specs, bool passed = true, Guid? waferId = null) =>
    [
        new { specificationVersionId = Id(specs[0]), sampleNumber = 1,
            samplePoint = "Center", waferId, unit = "nm", valueNumeric = passed ? 10 : 21 },

        new { specificationVersionId = Id(specs[1]), sampleNumber = 1,
            samplePoint = "Center", waferId, valueBoolean = true },
        new { specificationVersionId = Id(specs[2]), sampleNumber = 1,
            samplePoint = "Center", waferId, valueText = "OK" }
    ];

    [Fact]
    public async Task Inspection_freezes_versions_snapshots_limits_and_creates_one_issue_and_hold()
    {
        var sample = await Setup(2, 1);
        var (template, specs) = await InspectionSetup(sample, new string('检', 200));
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"quality/specifications/{Id(specs[0])}", new
        {
            parameterCode = specs[0].GetProperty("parameterCode").GetString(),
            version = 1,
            valueType = "decimal",
            unit = "nm",
            specMin = 0,
            specMax = 100
        }, E(specs[0]), method: "PUT")).StatusCode);
        var lot = await QLot(sample);
        var key = Guid.NewGuid().ToString();
        var body = new
        {
            lotId = Id(lot),
            lotEtag = E(lot),
            qualityCheckTemplateId = Id(template),
            measurements = MeasurementRows(specs, false)
        };
        var response = await Send("quality/checks", body, key: key);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var check = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(check.GetProperty("allPassed").GetBoolean());
        Assert.Equal(10, check.GetProperty("measurements").EnumerateArray()
            .Single(x => x.GetProperty("parameterType").GetString() == "decimal").GetProperty("specMin").GetDecimal());
        Assert.True((await Send("quality/checks", body, key: key)).Headers.Contains("Idempotency-Replayed"));
        lot = await QLot(sample);
        Assert.Single(lot.GetProperty("holds").EnumerateArray());
        var issue = await Get($"quality/issues/{check.GetProperty("issueId").GetGuid()}");
        Assert.True(issue.GetProperty("isBlocking").GetBoolean());
        await Ok("quality/checks", new
        {
            lotId = Id(lot),
            lotEtag = E(lot),
            qualityCheckTemplateId = Id(template),
            measurements = MeasurementRows(specs)
        });
        Assert.Equal(JsonValueKind.Null,
            (await QLot(sample)).GetProperty("holds")[0].GetProperty("releasedAt").ValueKind);
        await using var db = f.Database.CreateContext();
        var original = await db.Set<Measurement>().FirstAsync(x => x.QualityCheckId == Id(check));
        var waferId = Id(lot.GetProperty("wafers")[0]);
        var path = $"/api/v1/traceability/wafer/{waferId}/events";
        var timeline = await f.Admin.GetFromJsonAsync<PageView<TraceEvent>>($"{path}?group=quality");
        var measurement = Assert.Single(timeline!.Items, x => x.SourceId == original.Id);
        Assert.StartsWith("批级", measurement.Summary);
        Assert.Contains(timeline.Items, x => x.Type == "hold");
        var evidence = await f.Admin.GetFromJsonAsync<TraceEvidence>($"{path}/measurement/{original.Id}");
        Assert.Contains(evidence!.Fields, x => x.Label == "测量值");
        Assert.Contains(evidence.Fields, x => x.Label == "检验记录 ID" && x.Value == Id(check).ToString());
        original.IsPassed = !original.IsPassed;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Inspection_rejects_missing_duplicate_wrong_units_and_cross_fab_context_atomically()
    {
        var sample = await Setup(1, 1);
        var (template, specs) = await InspectionSetup(sample);
        var lot = await QLot(sample);
        foreach (var rows in new[]
        {
            MeasurementRows(specs).Take(2).ToArray(),
            new[] { MeasurementRows(specs)[0], MeasurementRows(specs)[0], MeasurementRows(specs)[2] },
            new object[] { new { specificationVersionId = Id(specs[0]),
                sampleNumber = 1, valueNumeric = 15, unit = "wrong" }, MeasurementRows(specs)[1],
                MeasurementRows(specs)[2] },
            new object[] { new { specificationVersionId = Id(specs[0]),
                sampleNumber = 1, valueNumeric = double.MaxValue, unit = "nm" },
                MeasurementRows(specs)[1], MeasurementRows(specs)[2] }
        })
        {
            var response = await Send("quality/checks", new
            {
                lotId = Id(lot),
                lotEtag = E(lot),
                qualityCheckTemplateId = Id(template),
                measurements = rows
            });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }
        var foreign = await f.Database.SeedAsync();
        var invalidIssue = await Ok("quality/issue-types", new
        {
            code = Guid.NewGuid().ToString("N"),
            name = "普通异常",
            severity = "Minor",
            isBlocking = false
        });
        Assert.Equal(HttpStatusCode.NotFound, (await Send("quality/issues", new
        {
            issueTypeId = Id(invalidIssue),
            title = "跨厂",
            description = "拒绝",
            equipmentId = foreign.Equipment
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await f.Admin.GetAsync($"/api/v1/quality/lots/{foreign.Lot}")).StatusCode);
        await using var db = f.Database.CreateContext();
        Assert.False(await db.Set<QualityCheck>().AnyAsync(x => x.LotId == Id(lot)));
        var waferId = Id(lot.GetProperty("wafers")[0]);
        var passed = await Ok("quality/checks", new
        {
            lotId = Id(lot),
            lotEtag = E(lot),
            qualityCheckTemplateId = Id(template),
            measurements = MeasurementRows(specs, waferId: waferId)
        });
        Assert.True(passed.GetProperty("allPassed").GetBoolean());
    }

    [Fact]
    public async Task Partial_scrap_releases_carrier_then_remaining_passed_wafers_complete()
    {
        var sample = await Setup(3, 1);
        var lot = await QLot(sample);
        var waferIds = lot.GetProperty("wafers").EnumerateArray().Select(Id).ToArray();
        var carrier = await Ok("production/carriers", new
        {
            carrierCode = Guid.NewGuid().ToString("N"),
            carrierType = "FOUP",
            slotCapacity = 3,
            allowedWaferSizeMm = 300
        });
        var carrierDetail = await Get($"production/carriers/{Id(carrier)}");
        var slots = carrierDetail.GetProperty("slots").EnumerateArray().Select(Id).ToArray();
        await Ok("production/carrier-load-commands", new
        {
            carrierVersions = new[] { new { id = Id(carrier), etag = E(carrier) } },
            waferVersions = lot.GetProperty("wafers").EnumerateArray().Select(w => new { id = Id(w), etag = E(w) }),
            lotVersions = new[] { new { id = Id(lot), etag = E(lot) } },
            moves = waferIds.Select((id, i) => new { waferId = id, fromSlotId = (Guid?)null, toSlotId = slots[i] }),
            reason = "报废装载测试"
        });
        var run = await Start(sample);
        var result = await End(run, true);
        var failed = (await Get($"execution/runs/{Id(run)}")).GetProperty("wafers").EnumerateArray()
            .Single(x => x.GetProperty("result").GetString() == "Failed");
        lot = await QLot(sample);
        var reason = await Reason("Scrap");
        var entry = await Ok($"quality/lots/{Id(lot)}/scrap", new
        {
            reasonCodeId = Id(reason),
            waferIds = new[] { Id(failed) },
            quantity = 1,
            notes = "报废不合格片"
        }, E(lot));
        await Release(sample, "Continue");
        var progress = await Get($"planning/work-orders/{Id(sample.Order)}/progress");
        Assert.Equal(3, progress.GetProperty("releasedQty").GetInt32());
        Assert.Equal(2, progress.GetProperty("completedQty").GetInt32());
        Assert.Equal(1, progress.GetProperty("scrappedQty").GetInt32());
        Assert.Equal(0, progress.GetProperty("inProcessQty").GetInt32());
        await using var db = f.Database.CreateContext();
        Assert.False(await db.Set<LotMembership>().AnyAsync(x => x.WaferId == Id(failed) && x.ValidTo == null));
        Assert.False(await db.Set<CarrierLoad>().AnyAsync(x => x.WaferId == Id(failed) && x.UnloadedAt == null));
        Assert.Equal(Id(sample.Lot),
            (await db.Set<WaferDisposition>().SingleAsync(x => x.ScrapEntryId == Id(entry))).LotIdAtEvent);
        var waferTrace = await f.Admin.GetFromJsonAsync<PageView<TraceEvent>>(
            $"/api/v1/traceability/wafer/{Id(failed)}/events");
        Assert.Contains(waferTrace!.Items, x => x.Type == "disposition" && x.Result == "Scrap");
        var carrierTrace = await f.Admin.GetFromJsonAsync<PageView<TraceEvent>>(
            $"/api/v1/traceability/carrier/{Id(carrier)}/events");
        Assert.Contains(carrierTrace!.Items, x => x.Type == "run-started" && x.SourceId == Id(run));
        Assert.Contains(carrierTrace.Items, x => x.Type == "carrier-unloaded" && x.Related.Any(l => l.Id == Id(failed)));
    }

    [Fact]
    public async Task All_scrap_closes_lot_without_completion_and_invalid_sets_leave_no_partial_changes()
    {
        var sample = await Setup(2, 1);
        var reason = await Reason("Scrap");
        var lot = await QLot(sample);
        var ids = lot.GetProperty("wafers").EnumerateArray().Select(Id).ToArray();
        foreach (var invalid in new[] { (ids, 1), (new[] { ids[0],
            ids[0] }, 2), (new[] { ids[0], Guid.NewGuid() }, 2) })
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Send($"quality/lots/{Id(lot)}/scrap", new
            {
                reasonCodeId = Id(reason),
                waferIds = invalid.Item1,
                quantity = invalid.Item2,
                notes = "非法集合"
            }, E(lot))).StatusCode);
        await using (var db = f.Database.CreateContext())
            Assert.False(await db.Set<ScrapEntry>().AnyAsync(x => x.WorkOrderId == Id(sample.Order)));
        await Ok($"quality/lots/{Id(lot)}/scrap", new
        {
            reasonCodeId = Id(reason),
            waferIds = ids,
            quantity = 2,
            notes = "全报废"
        }, E(lot));
        lot = await QLot(sample);
        Assert.Equal("Closed", lot.GetProperty("status").GetString());
        Assert.Equal("AllScrapped", lot.GetProperty("closeReason").GetString());
        var progress = await Get($"planning/work-orders/{Id(sample.Order)}/progress");
        Assert.Equal(2, progress.GetProperty("scrappedQty").GetInt32());
        Assert.Equal(0, progress.GetProperty("completedQty").GetInt32());
        var order = await Get($"planning/work-orders/{Id(sample.Order)}");
        await Ok($"planning/work-orders/{Id(order)}/close", new { reason = "报废收尾" }, E(order));
    }
    [Fact]
    public async Task Quality_only_role_can_authorize_terminal_recovery_and_revocation_blocks_replay()
    {
        var sample = await Setup(1, 1);
        var run = await Start(sample);
        var hold = await Hold(sample, await Reason("Hold"));
        await End(run);
        await ReleaseHold(hold);
        var roles = await Get("security/roles?pageSize=100");
        var role = roles.GetProperty("items").EnumerateArray()
            .Single(x => x.GetProperty("name").GetString() == "Viewer");
        role = await Ok($"security/roles/{Id(role)}/permissions", new
        {
            permissionCodes = new[] { "quality.read", "quality.release" },
            reason = "最小质量权限"
        }, E(role), "PUT");
        var quality = await f.NewUser();
        var me = await quality.Client.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
        Assert.DoesNotContain(me.GetProperty("permissions").EnumerateArray(),
            x => x.GetString() == "execution.track-out");
        var lot = await QLot(sample);
        var key = Guid.NewGuid().ToString();
        var path = $"quality/lots/{Id(lot)}/release";
        var body = new { action = "Continue", reason = "质量角色审核" };
        var result = await Send(path, body, E(lot), key, quality.Client);
        Assert.True(result.IsSuccessStatusCode, await result.Content.ReadAsStringAsync());
        var unscoped = await f.NewUser(scope: false);
        Assert.Equal(HttpStatusCode.Forbidden, (await unscoped.Client.GetAsync("/api/v1/quality/lots")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send("quality/reason-codes", new
        {
            code = "NO",
            name = "不允许",
            category = "Hold"
        }, client: quality.Client)).StatusCode);
        role = await Get($"security/roles/{Id(role)}");
        await Ok($"security/roles/{Id(role)}/permissions", new
        {
            permissionCodes = Array.Empty<string>(),
            reason = "撤权"
        }, E(role), "PUT");
        await OrganizationIdentityAccessControlFixture.Csrf(quality.Client);
        var denied = await Send(path, body, E(lot), key, quality.Client);
        Assert.Contains(denied.StatusCode, new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
    }

    [Fact]
    public async Task Scrap_replay_versions_and_concurrent_competition_do_not_duplicate_dispositions()
    {
        var sample = await Setup(1, 1);
        var reason = await Reason("Scrap");
        var lot = await QLot(sample);
        var body = new
        {
            reasonCodeId = Id(reason),
            waferIds = new[] { Id(lot.GetProperty("wafers")[0]) },
            quantity = 1,
            notes = "并发报废"
        };
        var path = $"quality/lots/{Id(lot)}/scrap";
        Assert.Equal((HttpStatusCode)428, (await Send(path, body)).StatusCode);
        var key = Guid.NewGuid().ToString();
        var results = await Task.WhenAll(Send(path, body, E(lot), key), Send(path, body, E(lot)));
        Assert.Single(results, x => x.IsSuccessStatusCode);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.PreconditionFailed);
        if (results[0].IsSuccessStatusCode)
        {
            Assert.True((await Send(path, body, E(lot), key)).Headers.Contains("Idempotency-Replayed"));
            Assert.Equal(HttpStatusCode.Conflict, (await Send(path,
                new { body.reasonCodeId, body.waferIds, quantity = 1, notes = "异参" },
                E(lot), key)).StatusCode);
        }
        await using var db = f.Database.CreateContext();
        Assert.Single(await db.Set<WaferDisposition>().Where(x => x.LotIdAtEvent == Id(lot)).ToListAsync());
    }

    [Fact]
    public async Task Scrap_audit_failure_rolls_back_lifecycle_membership_and_dispositions()
    {
        var sample = await Setup(2, 1);
        var reason = await Reason("Scrap");
        var lot = await QLot(sample);
        await using var db = f.Database.CreateContext();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION audit.fail_quality_test() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN IF NEW.entity_type='ScrapEntry' THEN RAISE EXCEPTION 'test failure' USING ERRCODE='23514'; END IF;
            RETURN NEW; END $$;
            CREATE TRIGGER test_quality_failure BEFORE INSERT ON audit.audit_logs
            FOR EACH ROW EXECUTE FUNCTION audit.fail_quality_test();
            """);
        try
        {
            var response = await Send($"quality/lots/{Id(lot)}/scrap", new
            {
                reasonCodeId = Id(reason),
                waferIds = lot.GetProperty("wafers").EnumerateArray().Select(Id),
                quantity = 2,
                notes = "回滚测试"
            }, E(lot));
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.False(await db.Set<ScrapEntry>().AnyAsync(x => x.WorkOrderId == Id(sample.Order)));
            Assert.Equal(2, await db.Set<LotMembership>().CountAsync(x => x.LotId == Id(lot) && x.ValidTo == null));
            Assert.Equal(2, await db.Set<Wafer>().CountAsync(x => x.WorkOrderId == Id(sample.Order)
                && x.LifecycleStatus == "Active"));
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("DROP TRIGGER test_quality_failure ON audit.audit_logs; DROP FUNCTION audit.fail_quality_test();");
        }
    }
#pragma warning disable EF1002
    [Fact]
    public async Task Runtime_role_can_scrap_and_complete_quality_recovery_without_ddl_or_delete()
    {
        var sample = await Setup(2, 1);
        await End(await Start(sample), true);
        var reason = await Reason("Scrap");
        var actorId = (await Get("auth/me")).GetProperty("userId").GetGuid();
        var role = "mesagent_test_quality_" + Guid.NewGuid().ToString("N");
        await using var db = f.Database.CreateContext();
        await db.Database.ExecuteSqlRawAsync($"CREATE ROLE {role} NOLOGIN");
        try
        {
            await db.Database.ExecuteSqlRawAsync($"""
                GRANT USAGE ON SCHEMA organization,security,engineering,production,equipment,quality,audit TO {role};
                GRANT SELECT ON ALL TABLES IN SCHEMA organization,security,engineering,production,equipment,quality,audit TO {role};
                GRANT UPDATE ON production.lots,production.wafers,production.lot_memberships,production.carrier_loads,
                    production.step_executions,production.work_orders,production.carriers TO {role};
                GRANT INSERT ON production.wafer_completions,audit.audit_logs,audit.business_events,
                    audit.command_receipts,quality.scrap_entries,quality.wafer_dispositions,quality.lot_releases TO {role};
                """);
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                await db.Database.ExecuteSqlRawAsync($"SET LOCAL ROLE {role}");
                await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(78001001)");
                var backend = new QualityBackend(db, f.Fab, new RequestActor(actorId, Guid.NewGuid(), Guid.NewGuid()),
                    p => Assert.StartsWith("quality.", p), _ => true, TimeProvider.System);
                async Task<JsonElement> Execute(string method, string path, object body, string? version = null)
                {
                    var result = await backend.Execute(new(method, path, JsonSerializer.SerializeToElement(body),
                        new Dictionary<string, string>(), version, Guid.NewGuid().ToString()));
                    await db.SaveChangesAsync();
                    return JsonSerializer.SerializeToElement(result.Data);
                }
                var lot = await Execute("GET", $"quality/lots/{Id(sample.Lot)}", new { });
                var failed = await (from wafer in db.Set<ProcessRunWafer>()
                                    join run in db.Set<ProcessRun>() on wafer.ProcessRunId equals run.Id
                                    join step in db.Set<StepExecution>() on run.StepExecutionId equals step.Id
                                    where step.LotId == Id(sample.Lot) && wafer.Result == "Failed"
                                    select wafer.WaferId).SingleAsync();
                await Execute("POST", $"quality/lots/{Id(lot)}/scrap", new
                {
                    reasonCodeId = Id(reason),
                    waferIds = new[] { failed },
                    quantity = 1,
                    notes = "运行角色报废"
                }, E(lot));
                lot = await Execute("GET", $"quality/lots/{Id(lot)}", new { });
                var result = await Execute("POST", $"quality/lots/{Id(lot)}/release", new
                {
                    action = "Continue",
                    reason = "运行角色质量审核"
                }, E(lot));
                Assert.Equal("Completed", result.GetProperty("lot").GetProperty("status").GetString());
                Assert.False(await db.Database.SqlQueryRaw<bool>(
                    "SELECT has_schema_privilege(current_user,'quality','CREATE') AS \"Value\"").SingleAsync());
                Assert.False(await db.Database.SqlQueryRaw<bool>(
                    "SELECT has_table_privilege(current_user,'quality.scrap_entries','DELETE') AS \"Value\"").SingleAsync());
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
