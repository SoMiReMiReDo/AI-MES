using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MESagent.Api.Tests;

public sealed partial class ProductionTests
{
    private async Task<JsonElement> IncomingWaferLot()
    {
        var code = Guid.NewGuid().ToString("N");
        var material = await Ok("materials/wafer-materials", new { code, name = "测试硅片" });
        var source = await Ok("materials/wafer-sources", new
        {
            materialId = Id(material),
            code,
            name = "演示来源"
        });
        return await Ok("materials/wafer-lots", new { sourceId = Id(source), lotNumber = code, supplierLotNo = code });
    }

    [Fact]
    public async Task Incoming_wafer_origin_traces_both_directions_after_split_without_stock()
    {
        var source = await IncomingWaferLot();
        var path = $"materials/wafer-lots/{Id(source)}";
        var number = source.GetProperty("lotNumber").GetString();
        var options = await Get($"materials/wafer-origin-options?search={number}");
        Assert.Equal(Id(source), Id(Assert.Single(options.GetProperty("items").EnumerateArray())));
        var lot = await Ok("production/lots", new
        {
            workOrderId = Id(await Order(2)),
            lotNumber = Guid.NewGuid().ToString("N"),
            wafers = new[]
            {
                new { waferCode = Guid.NewGuid().ToString("N"), waferSizeMm = 300,
                    originMaterialLotId = (Guid?)Id(source), supplierWaferCode = "SUPPLIER-01" },
                new { waferCode = Guid.NewGuid().ToString("N"), waferSizeMm = 300,
                    originMaterialLotId = (Guid?)null, supplierWaferCode = "" }
            }
        });
        var waferId = lot.GetProperty("waferIds")[0].GetGuid();
        var wafer = await Get($"production/wafers/{waferId}");
        Assert.Equal(number, wafer.GetProperty("originMaterialLot").GetProperty("lotNumber").GetString());
        var legacy = await Get($"production/wafers/{lot.GetProperty("waferIds")[1].GetGuid()}");
        Assert.Equal("Unregistered", legacy.GetProperty("sourceCoverage").GetString());
        var released = await Release(lot);
        var split = await Ok($"production/lots/{Id(lot)}/split", new
        {
            waferIds = new[] { waferId },
            reason = "核对来料反查",
            newLotNumber = Guid.NewGuid().ToString("N")
        }, etag: E(released));
        var reverse = await Get($"{path}/wafers");
        var match = Assert.Single(reverse.GetProperty("items").EnumerateArray());
        Assert.Equal(waferId, Id(match));
        Assert.Equal("SUPPLIER-01", match.GetProperty("supplierWaferCode").GetString());
        Assert.Equal(Id(split.GetProperty("newLot")), match.GetProperty("currentLotId").GetGuid());

        await using var db = f.Database.CreateContext();
        var recorded = await db.Set<MaterialLot>().FindAsync(Id(source));
        Assert.Equal(0, recorded!.QuantityAvailable);
        Assert.Equal(0, recorded.QuantityReceived);
        Assert.False(await db.Set<WarehouseStock>().AnyAsync(x => x.MaterialLotId == recorded.Id));
        Assert.False(await db.Set<StockMovement>().AnyAsync(x => x.MaterialLotId == recorded.Id));
        var consumables = await Get("equipment/consumable-lots");
        Assert.DoesNotContain(consumables.GetProperty("items").EnumerateArray(), row => Id(row) == recorded.Id);
    }

    [Fact]
    public async Task Incoming_registration_replay_records_one_fact_and_role_cannot_bypass_permission()
    {
        var code = Guid.NewGuid().ToString("N");
        const string path = "materials/wafer-materials";
        var body = new { code, name = "演示硅片" };
        var key = Guid.NewGuid().ToString();
        var first = await Send(path, body, key: key);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var id = Id(await first.Content.ReadFromJsonAsync<JsonElement>());
        var replay = await Send(path, body, key: key);
        Assert.True(replay.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(id, Id(await replay.Content.ReadFromJsonAsync<JsonElement>()));
        Assert.Equal(HttpStatusCode.Conflict,
            (await Send(path, new { code, name = "异参" }, key: key)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Send(path, body)).StatusCode);

        var reader = await f.NewUser("Operator");
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(path, body, client: reader.Client)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await reader.Client.GetAsync("/api/v1/materials/wafer-lots")).StatusCode);
        await using var db = f.Database.CreateContext();
        Assert.Equal(1, await db.Set<Material>().CountAsync(x => x.Code == code));
        Assert.Equal(1, await db.Set<AuditLog>().CountAsync(x => x.EntityId == id && x.Action == "MaterialRegistered"));
        Assert.Equal(1, await db.Set<BusinessEvent>().CountAsync(x => x.EntityId == id
            && x.EventType == "Materials.MaterialRegistered"));
    }

    [Fact]
    public async Task Incoming_wafer_origin_blocking_is_enforced_again_at_release_and_across_fabs()
    {
        var source = await IncomingWaferLot();
        var lotId = Id(source);
        var order = await Order(2);
        object CreateBody() => new
        {
            workOrderId = Id(order),
            lotNumber = Guid.NewGuid().ToString("N"),
            wafers = new[] { new { waferCode = Guid.NewGuid().ToString("N"), waferSizeMm = 300,
                originMaterialLotId = lotId } }
        };
        var draft = await Ok("production/lots", CreateBody());
        await using var db = f.Database.CreateContext();
        var lot = (await db.Set<MaterialLot>().FindAsync(lotId))!;
        lot.HeldAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var options = await Get($"materials/wafer-origin-options?search={lot.LotNumber}");
        Assert.Empty(options.GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.Conflict, (await Send("production/lots", CreateBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await Send($"production/lots/{Id(draft)}/release", new { }, etag: E(draft))).StatusCode);
        Assert.False(await db.Set<WaferRelease>().AnyAsync(x => x.WorkOrderId == Id(order)));

        var otherFab = new Fab { Code = Guid.NewGuid().ToString("N"), Name = "其他厂区", Timezone = "UTC" };
        db.Add(otherFab);
        (await db.Set<MaterialSource>().FindAsync(lot.SourceId))!.ManualFabId = otherFab.Id;
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync($"/api/v1/materials/wafer-lots/{lotId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await f.Admin.GetAsync($"/api/v1/materials/wafer-lots/{lotId}/wafers")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send("production/lots", CreateBody())).StatusCode);
    }
}
