using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MESagent.Api.Tests;

public sealed partial class ExecutionTests
{
    private const string SimulatorConnections = "integration/connections";
    private const string WorkOrderImports = "integration/work-order-imports";

    [Fact]
    public async Task Simulator_inbox_deduplicates_by_connection_and_preserves_F7_state_and_run()
    {
        var sample = await Setup(stations: 1);
        var run = await Start(sample);
        var first = await Ok(SimulatorConnections, new { equipmentId = Id(sample.Tool), name = "模拟 A" });
        var second = await Ok(SimulatorConnections, new { equipmentId = Id(sample.Tool), name = "模拟 B" });
        var a = $"{SimulatorConnections}/{Id(first)}/messages";
        var b = $"{SimulatorConnections}/{Id(second)}/messages";
        var now = DateTime.UtcNow;
        var eventTime = now.AddTicks(-(now.Ticks % 10) + 1);
        var rawPayload = SimulatorJson(eventTime, "State", "Down");
        var reply = await Task.WhenAll(
            Send(a, new { sourceMessageId = "1", rawPayload }),
            Send(a, new { sourceMessageId = "1", rawPayload }));
        Assert.Contains(reply, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Contains(reply, r => r.StatusCode == HttpStatusCode.OK
            && r.Headers.Contains("Idempotency-Replayed"));
        var delivered = await reply[0].Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Id(delivered), Id(await reply[1].Content.ReadFromJsonAsync<JsonElement>()));
        Assert.Equal("Applied", delivered.GetProperty("status").GetString());
        Assert.Equal(rawPayload, (await Get($"{a}/{Id(delivered)}")).GetProperty("rawPayload").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await Send(a,
            new { sourceMessageId = "1", rawPayload = rawPayload + " " })).StatusCode);

        var old = await Ok(b, new { sourceMessageId = "1", rawPayload = SimulatorJson(eventTime, "State", "Idle") });
        Assert.Equal("Rejected", old.GetProperty("status").GetString());
        Assert.Contains("乱序", old.GetProperty("error").GetString());
        var connected = await Ok(b, new
        {
            sourceMessageId = "2",
            rawPayload = SimulatorJson(DateTime.UtcNow, "Connection", "Online")
        });
        Assert.Equal("Applied", connected.GetProperty("status").GetString());
        var currentConnection = await Get($"{SimulatorConnections}/{Id(second)}");
        Assert.Equal("Online", currentConnection.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.String, currentConnection.GetProperty("lastConnectedAt").ValueKind);
        var malformed = await Ok(a, new { sourceMessageId = "2", rawPayload = "{invalid" });
        Assert.Equal("Rejected", malformed.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, malformed.GetProperty("equipmentEventId").ValueKind);
        var invalid = await Ok(a, new
        {
            sourceMessageId = "3",
            rawPayload = SimulatorJson(DateTime.UtcNow, "State", "Running")
        });
        Assert.Equal("Rejected", invalid.GetProperty("status").GetString());
        Assert.Equal(3, (await Get(a)).GetProperty("total").GetInt32());
        Assert.Equal("Down", (await EquipmentRow(sample)).GetProperty("operationalState").GetString());
        Assert.Equal("Online", (await EquipmentRow(sample)).GetProperty("communicationState").GetString());
        Assert.Equal(1, (await Get($"equipment/downtimes?equipmentId={Id(sample.Tool)}"))
            .GetProperty("total").GetInt32());
        await using var db = f.Database.CreateContext();
        Assert.True(await db.Set<ProcessRun>().AnyAsync(x => x.Id == Id(run) && x.Status == "Running"));
        var eventId = delivered.GetProperty("equipmentEventId").GetGuid();
        Assert.True(await db.Set<EquipmentEvent>().AnyAsync(x => x.Id == eventId && x.MachineConnectionId == Id(first)
            && x.EquipmentId == Id(sample.Tool) && x.IsSimulated));
    }

    [Fact]
    public async Task Work_order_csv_maps_columns_checks_records_and_confirms_drafts_once()
    {
        var (product, _) = await ImportProduct();
        var orderNo = Guid.NewGuid().ToString("N");
        var csv = "\uFEFF单号,产品,修订,数量,备注,交期,计划\r\n"
            + $"{orderNo},{product},A,12,\"备注, \"\"复核\"\"\r\n第二行\",2026-11-30,2026-11-20T16:30:00+08:00\r\n"
            + $"{orderNo}-2,{product},A,5,,,\r\n";
        var body = new
        {
            filename = "工单.csv",
            csv,
            mapping = new Dictionary<string, string>
            {
                ["orderNo"] = "单号",
                ["productCode"] = "产品",
                ["revisionCode"] = "修订",
                ["plannedQty"] = "数量",
                ["description"] = "备注",
                ["dueDate"] = "交期",
                ["plannedStartAt"] = "计划"
            }
        };
        var previewKey = Guid.NewGuid().ToString();
        var first = await Send(WorkOrderImports, body, key: previewKey);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var preview = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ready", preview.GetProperty("status").GetString());
        Assert.Equal(2, preview.GetProperty("totalRows").GetInt32());
        Assert.Equal(0, preview.GetProperty("errors").GetArrayLength());
        Assert.Equal(0, (await Get($"planning/work-orders?orderNo={orderNo}")).GetProperty("total").GetInt32());
        var previewReplay = await Send(WorkOrderImports, body, key: previewKey);
        Assert.True(previewReplay.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(Id(preview), Id(await previewReplay.Content.ReadFromJsonAsync<JsonElement>()));

        var path = $"{WorkOrderImports}/{Id(preview)}/confirm";
        Assert.Equal((HttpStatusCode)428, (await Send(path, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Send(path, new { }, "\"stale\"")).StatusCode);
        var confirmKey = Guid.NewGuid().ToString();
        var confirmations = await Task.WhenAll(
            Send(path, new { }, E(preview), confirmKey), Send(path, new { }, E(preview), confirmKey));
        Assert.All(confirmations, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        Assert.Single(confirmations, response => response.Headers.Contains("Idempotency-Replayed"));
        var result = await confirmations[0].Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Imported", result.GetProperty("status").GetString());
        Assert.Equal(2, result.GetProperty("successfulRows").GetInt32());
        Assert.Equal(2, (await Get($"planning/work-orders?orderNo={orderNo}")).GetProperty("total").GetInt32());
        var id = Id(result.GetProperty("orders")[0]);
        var order = await Get($"planning/work-orders/{id}");
        Assert.Equal("Draft", order.GetProperty("status").GetString());
        Assert.Equal(12, order.GetProperty("plannedQty").GetInt32());
        Assert.Equal("2026-11-30", order.GetProperty("dueDate").GetString());
        Assert.Equal(new DateTime(2026, 11, 20, 8, 30, 0, DateTimeKind.Utc),
            order.GetProperty("plannedStartAt").GetDateTime());
        Assert.Equal("备注, \"复核\"\n第二行", order.GetProperty("description").GetString()!.Replace("\r\n", "\n"));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(path, new { }, E(result))).StatusCode);
        Assert.Equal("Imported", (await Get($"{WorkOrderImports}/{Id(preview)}")).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Work_order_csv_invalid_rows_and_late_conflict_create_no_partial_orders()
    {
        var (product, revision) = await ImportProduct();
        var prefix = Guid.NewGuid().ToString("N");
        const string header = "orderNo,productCode,revisionCode,plannedQty,priority";
        var rejected = await Ok(WorkOrderImports, new
        {
            filename = "invalid.csv",
            csv = $"{header}\n{prefix}-a,{product},A,5,10\n{prefix}-a,{product},A,5,10\n"
                + $"{prefix}-b,{product},A,0,101\n{prefix}-c,{product},unknown,5,10\n",
            mapping = new { }
        });
        Assert.Equal("Rejected", rejected.GetProperty("status").GetString());
        Assert.Equal(4, rejected.GetProperty("failedRows").GetInt32());
        Assert.Equal(5, rejected.GetProperty("errors").GetArrayLength());
        Assert.Equal(HttpStatusCode.Conflict,
            (await Send($"{WorkOrderImports}/{Id(rejected)}/confirm", new { }, E(rejected))).StatusCode);
        Assert.Equal(0, (await Get($"planning/work-orders?orderNo={prefix}")).GetProperty("total").GetInt32());

        var ready = await Ok(WorkOrderImports, new
        {
            filename = "late.csv",
            csv = $"{header}\n{prefix}-1,{product},A,5,10\n{prefix}-2,{product},A,10,20",
            mapping = new { }
        });
        await Ok("planning/work-orders", new
        {
            orderNo = prefix + "-2",
            productId = revision.GetProperty("productId").GetGuid(),
            productRevisionId = Id(revision),
            plannedQty = 1
        });

        var result = await Ok($"{WorkOrderImports}/{Id(ready)}/confirm", new { }, E(ready));
        Assert.Equal("Rejected", result.GetProperty("status").GetString());
        Assert.Equal(0, result.GetProperty("successfulRows").GetInt32());
        Assert.Equal(3, result.GetProperty("errors")[0].GetProperty("row").GetInt32());
        Assert.Equal(0, (await Get($"planning/work-orders?orderNo={prefix}-1")).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Integration_enforces_permissions_scope_and_import_ownership_before_lookup_and_replay()
    {
        var (viewer, _, _) = await f.NewUser();
        using var viewerClient = viewer;
        var (otherAdmin, _, _) = await f.NewUser("SystemAdmin");
        using var otherClient = otherAdmin;
        var (unscoped, _, _) = await f.NewUser("SystemAdmin", false);
        using var outside = unscoped;
        var body = new { filename = "ownership.csv", csv = "unknown", mapping = new { } };
        var key = Guid.NewGuid().ToString();
        var preview = await Send(WorkOrderImports, body, key: key);
        var id = Id(await preview.Content.ReadFromJsonAsync<JsonElement>());

        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync($"/api/v1/{SimulatorConnections}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Send(WorkOrderImports, body, key: key, client: viewer)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await unscoped.GetAsync($"/api/v1/{SimulatorConnections}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await otherAdmin.GetAsync($"/api/v1/{WorkOrderImports}/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await Send($"{WorkOrderImports}/{id}/confirm", new { }, client: otherAdmin)).StatusCode);
        Assert.Equal(0, (await otherAdmin.GetFromJsonAsync<JsonElement>($"/api/v1/{WorkOrderImports}"))
            .GetProperty("total").GetInt32());
    }

    private async Task<(string Code, JsonElement Revision)> ImportProduct()
    {
        var code = Guid.NewGuid().ToString("N");
        var product = await Ok("engineering/products", new { code, name = "导入测试产品", unitOfMeasure = "wafer" });
        var revision = await Ok("engineering/product-revisions", new { productId = Id(product), revisionCode = "A" });
        return (code, revision);
    }

    private static string SimulatorJson(DateTime occurredAt, string kind, string state) =>
        JsonSerializer.Serialize(new { occurredAt, kind, state, reason = "F11 模拟" });
}
