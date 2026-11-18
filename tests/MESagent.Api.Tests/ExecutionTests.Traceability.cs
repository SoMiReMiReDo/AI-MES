using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using Xunit;

namespace MESagent.Api.Tests;

public sealed partial class ExecutionTests
{
    private Task<PageView<TraceEvent>?> TraceEvents(string path, string query = "") =>
        f.Admin.GetFromJsonAsync<PageView<TraceEvent>>($"/api/v1/traceability/{path}/events{query}");

    [Fact]
    public async Task Traceability_keeps_the_actual_run_with_the_original_lot_after_a_split()
    {
        var sample = await Setup(count: 2, stations: 2);
        var original = await Start(sample);
        await End(original);
        var lot = await Get($"production/lots/{Id(sample.Lot)}");
        var wafer = (await Get($"production/lots/{Id(lot)}/wafers")).GetProperty("items")[0];
        var split = await Ok($"production/lots/{Id(lot)}/split", new
        {
            waferIds = new[] { Id(wafer) }, reason = "拆批追溯", newLotNumber = Guid.NewGuid().ToString("N")
        }, E(lot));
        var child = split.GetProperty("newLot");
        var successor = await Start(sample, child);
        await End(successor);

        foreach (var (kind, subject, key) in new[]
        {
            ("work-order", sample.Order, "orderNo"), ("lot", lot, "lotNumber"),
            ("wafer", wafer, "waferCode"), ("equipment", sample.Tool, "code")
        })
        {
            var code = subject.GetProperty(key).GetString()!;
            var match = await f.Admin.GetFromJsonAsync<PageView<TraceObject>>(
                $"/api/v1/traceability/search?kind={kind}&q={code.ToUpperInvariant()}");
            Assert.Equal(Id(subject), Assert.Single(match!.Items).Id);
            var timeline = await TraceEvents($"{kind}/{Id(subject)}", "?pageSize=100");
            Assert.Contains(timeline!.Items, x => x.Type == "run-started" && x.SourceId == Id(original));
        }
        var waferPath = $"wafer/{Id(wafer)}";
        var waferTimeline = (await TraceEvents(waferPath, "?pageSize=100"))!.Items;
        var edge = Assert.Single(waferTimeline, item => item.Type == "genealogy");
        Assert.Contains(edge.Related, link => link.Relation == "来源批" && link.Id == Id(lot));
        Assert.Contains(edge.Related, link => link.Relation == "目标批" && link.Id == Id(child));
        Assert.Contains((await TraceEvents($"lot/{Id(lot)}", "?group=genealogy"))!.Items, x => x.SourceId == edge.SourceId);
        Assert.Contains((await TraceEvents($"lot/{Id(child)}", "?group=genealogy"))!.Items, x => x.SourceId == edge.SourceId);
        Assert.DoesNotContain((await TraceEvents($"lot/{Id(child)}", "?group=execution"))!.Items,
            x => x.SourceId == Id(original));

        var fact = Assert.Single(waferTimeline, x => x.Type == "run-started" && x.SourceId == Id(original));
        Assert.Contains(fact.Related, x => x.Kind == "lot" && x.Id == Id(lot));
        var evidence = await f.Admin.GetFromJsonAsync<TraceEvidence>(
            $"/api/v1/traceability/{waferPath}/events/run-started/{fact.SourceId}");
        Assert.Contains(evidence!.Fields, x => x.Label == "实际配方版本 ID" && x.Value == Id(sample.Recipe).ToString());
        Assert.Contains(evidence.Fields, x => x.Label == "逐片结果" && x.Value == "Passed");
        Assert.False(string.IsNullOrWhiteSpace(evidence.Event.Actor));
        Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync(
            $"/api/v1/traceability/lot/{Id(child)}/events/run-started/{fact.SourceId}")).StatusCode);

        var from = Uri.EscapeDataString(fact.OccurredAt.ToString("O", CultureInfo.InvariantCulture));
        var firstPage = await TraceEvents(waferPath, $"?group=execution&from={from}&pageSize=1");
        var secondPage = await TraceEvents(waferPath, $"?group=execution&from={from}&pageSize=1&page=2");
        Assert.Equal(firstPage!.Total, secondPage!.Total);
        Assert.NotEqual(firstPage.Items[0], secondPage.Items[0]);
        var before = await TraceEvents(waferPath, $"?group=execution&to={from}");
        Assert.DoesNotContain(before!.Items, x => x.Type == fact.Type && x.SourceId == fact.SourceId);
        Assert.Contains((await TraceEvents(waferPath, $"?group=execution&from={from}"))!.Items,
            x => x.Type == fact.Type && x.SourceId == fact.SourceId);
    }

    [Fact]
    public async Task Traceability_search_and_evidence_obey_root_scope_and_source_permissions()
    {
        var sample = await Setup(count: 1, stations: 1);
        var run = await Start(sample);
        var path = $"/api/v1/traceability/lot/{Id(sample.Lot)}";
        var noScope = await f.NewUser("SystemAdmin", scope: false);
        Assert.Equal(HttpStatusCode.Forbidden, (await noScope.Client.GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.Client().GetAsync(path)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Admin.GetAsync($"{path}/events?page=invalid")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Admin.GetAsync($"{path}/events?from=2026-11-18")).StatusCode);

        var role = "Trace-" + Guid.NewGuid().ToString("N")[..12];
        await Ok("security/roles", new { name = role, permissionCodes = new[] { "production.lots.read" } });
        var reader = await f.NewUser(role);
        var overview = await reader.Client.GetFromJsonAsync<TraceOverview>(path);
        Assert.Equal(new[] { "lots" }, overview!.Groups);
        var history = await reader.Client.GetFromJsonAsync<PageView<TraceEvent>>($"{path}/events");
        Assert.All(history!.Items, x => Assert.Equal("lot-created", x.Type));
        Assert.Equal(HttpStatusCode.NotFound,
            (await reader.Client.GetAsync($"{path}/events/run-started/{Id(run)}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.Client.GetAsync(
            "/api/v1/traceability/search?kind=equipment&q=anything")).StatusCode);

        await using var db = f.Database.CreateContext();
        var otherFab = new Fab { Code = Guid.NewGuid().ToString("N"), Name = "另一厂区", Timezone = "UTC" };
        var foreign = new Carrier { FabId = otherFab.Id, CarrierCode = "F09_%", CarrierType = "FOUP",
            SlotCapacity = 2, Status = "Active" };
        var local = new Carrier { FabId = f.Fab, CarrierCode = "F09_%LOCAL", CarrierType = "FOUP",
            SlotCapacity = 2, Status = "Active" };
        db.AddRange(otherFab, foreign, local);
        await db.SaveChangesAsync();
        var search = await f.Admin.GetFromJsonAsync<PageView<TraceObject>>(
            "/api/v1/traceability/search?kind=carrier&q=F09_%25");
        Assert.Equal(local.Id, Assert.Single(search!.Items).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await f.Admin.GetAsync(
            $"/api/v1/traceability/carrier/{foreign.Id}/events")).StatusCode);
        var carrier = await f.Admin.GetFromJsonAsync<TraceOverview>($"/api/v1/traceability/carrier/{local.Id}");
        Assert.Equal(local.Id, carrier!.Subject.Id);
        Assert.Empty((await TraceEvents($"carrier/{local.Id}"))!.Items);
    }
}
