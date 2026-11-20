using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MESagent.Api.Tests;

public sealed partial class ExecutionTests
{
    [Fact]
    public async Task Analytics_orders_reconcile_threshold_completion_paging_export_and_source_permissions()
    {
        var sample = await Setup(2, 1);
        var id = Id(sample.Order);
        var orderNo = sample.Order.GetProperty("orderNo").GetString()!;
        var day = FabDate();
        var parameters = $"from={day:yyyy-MM-dd}&to={day:yyyy-MM-dd}&q={orderNo}";
        await End(await Start(sample));

        var report = (await f.Admin.GetFromJsonAsync<AnalyticsOrders>(
            $"/api/v1/analytics/work-orders?{parameters}&pageSize=1"))!;
        var row = Assert.Single(report.Records.Items);
        Assert.Equal("reached", row.Event);
        Assert.Equal("Released", row.Status);
        Assert.Equal(2, row.Released);
        Assert.Equal(2, row.Completed);
        Assert.Null(row.OnTime);
        Assert.Equal(0, report.Summary.WithDueDate);
        Assert.Equal(row.CompletedAt, row.EventAt);
        Assert.Equal(2, report.Summary.Completed);
        var detail = (await f.Admin.GetFromJsonAsync<AnalyticsOrderDetail>($"/api/v1/analytics/work-orders/{id}"))!;
        Assert.Equal(2, detail.Baseline!.Planned);
        Assert.Single(detail.Lots!.Items);
        Assert.Contains(detail.Context.Evidence, x => x.Kind == "process-run");

        using var export = await Send($"analytics/work-orders/export?{parameters}", new { });
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("text/csv", export.Content.Headers.ContentType!.MediaType);
        Assert.Contains(orderNo, await export.Content.ReadAsStringAsync());
        await using (var db = f.Database.CreateContext())
        {
            var audit = await db.Set<AuditLog>().SingleAsync(x => x.Module == "analytics");
            Assert.Equal("Exported", audit.Action);
            Assert.NotEqual(Guid.Empty, audit.CorrelationId);
            Assert.Contains(orderNo, audit.AfterState);
        }

        var role = "AnalyticsOrders-" + Guid.NewGuid().ToString("N")[..8];
        await Ok("security/roles", new { name = role, permissionCodes = new[] { "planning.work-orders.read" } });
        using var reader = (await f.NewUser(role)).Client;
        var restrictedPath = $"/api/v1/analytics/work-orders/{id}";
        var restricted = (await reader.GetFromJsonAsync<AnalyticsOrderDetail>(restrictedPath))!;
        var restrictedJson = await reader.GetFromJsonAsync<JsonElement>(restrictedPath);
        Assert.False(restrictedJson.GetProperty("order").TryGetProperty("scrapped", out _));
        Assert.False(restrictedJson.TryGetProperty("lots", out _));
        Assert.Null(restricted.Order.Scrapped);
        Assert.Null(restricted.Lots);
        Assert.DoesNotContain(restricted.Context.Evidence, x => x.Kind is "lot" or "process-run" or "quality-issue");
        using var restrictedCsv = await Send($"analytics/work-orders/export?{parameters}", new { }, client: reader);
        Assert.Equal(HttpStatusCode.OK, restrictedCsv.StatusCode);
        Assert.DoesNotContain("报废片数", await restrictedCsv.Content.ReadAsStringAsync());
        using var unscoped = (await f.NewUser("SystemAdmin", scope: false)).Client;
        Assert.Equal(HttpStatusCode.Forbidden,
            (await unscoped.GetAsync($"/api/v1/analytics/work-orders/{id}")).StatusCode);

        var current = await Get($"planning/work-orders/{id}");
        await Ok($"planning/work-orders/{id}/close", new { reason = "分析测试" }, E(current));
        var closed = (await f.Admin.GetFromJsonAsync<AnalyticsOrders>($"/api/v1/analytics/work-orders?{parameters}"))!;
        var terminal = Assert.Single(closed.Records.Items);
        Assert.Equal("closed", terminal.Event);
        Assert.Equal(terminal.TerminalAt, terminal.EventAt);
        Assert.Equal(row.CompletedAt, terminal.CompletedAt);
    }

    [Fact]
    public async Task Analytics_scrap_pareto_uses_distinct_reasons_and_matches_dispositions_and_terminal_wafers()
    {
        var sample = await Setup(4, 1);
        var lotId = Id(sample.Lot);
        var product = sample.Order.GetProperty("productId").GetGuid();
        var reasons = new List<Guid>();
        for (var i = 0; i < 2; i++)
            reasons.Add(Id(await Ok("quality/reason-codes", new
            {
                code = Guid.NewGuid().ToString("N"),
                name = "分析同名原因",
                category = "Scrap"
            })));
        var quality = await Get($"quality/lots/{lotId}");
        var wafers = quality.GetProperty("wafers").EnumerateArray().Select(Id).ToArray();
        for (var i = 0; i < reasons.Count; i++)
        {
            var scrap = i == 0 ? wafers[..2] : wafers[2..3];
            quality = await Get($"quality/lots/{lotId}");
            await Ok($"quality/lots/{lotId}/scrap",
                new { reasonCodeId = reasons[i], waferIds = scrap, quantity = scrap.Length, notes = "分析样本" },
                E(quality));
        }
        await End(await Start(sample));

        var day = FabDate();
        var filters = $"from={day:yyyy-MM-dd}&to={day:yyyy-MM-dd}&productId={product}";
        var report = (await f.Admin.GetFromJsonAsync<AnalyticsScrap>($"/api/v1/analytics/scrap?{filters}"))!;
        Assert.Equal(3, report.Wafers);
        Assert.Equal(2, report.Entries);
        Assert.Equal(1, report.Completed);
        Assert.Equal(4, report.Settled);
        Assert.Equal(0.75, report.SettledScrapRatio);
        Assert.Equal(3, Assert.Single(report.Days).Wafers);
        Assert.Equal(reasons, report.Reasons.Select(x => x.Id!.Value));
        Assert.Equal(1, report.Reasons[^1].CumulativeRatio);
        Assert.Null(Assert.Single(report.Equipment!).Id);

        var parameters = $"{filters}&reasonId={reasons[0]}&pageSize=1";
        var rows = (await f.Admin.GetFromJsonAsync<AnalyticsScrapRecords>(
            $"/api/v1/analytics/scrap/records?{parameters}"))!;
        Assert.Equal(2, rows.Records.Total);
        var row = Assert.Single(rows.Records.Items);
        Assert.Equal(reasons[0], row.Reason.Id);
        Assert.Contains(row.Evidence, x => x.Kind == "quality-scrap" && x.Id == row.EntryId);

        var role = "AnalyticsScrap-" + Guid.NewGuid().ToString("N")[..8];
        await Ok("security/roles", new { name = role, permissionCodes = new[] { "quality.read" } });
        using var reader = (await f.NewUser(role)).Client;
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync(
            $"/api/v1/analytics/scrap?{filters}&equipmentId={Id(sample.Tool)}")).StatusCode);
        var restricted = (await reader.GetFromJsonAsync<AnalyticsScrap>($"/api/v1/analytics/scrap?{filters}"))!;
        Assert.Null(restricted.Equipment);
        Assert.Null(restricted.SettledScrapRatio);
        Assert.All(restricted.Context.Evidence, x => Assert.Equal("quality-scrap", x.Kind));
    }

    [Fact]
    public async Task Analytics_oee_matches_f10_and_withholds_overlapping_or_incomplete_aggregates()
    {
        var sample = await Setup(2, 1);
        var tool = Id(sample.Tool);
        var shift = Id(await RegisterFullShift());
        await RegisterFullShift();
        var date = FabDate();
        var filters = $"equipmentId={tool}&from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}";
        var run = await Start(sample);
        await Ok("workbench/rates", new { equipmentId = tool, effectiveOn = date, idealSeconds = 1 });
        var url = $"/api/v1/analytics/oee/shifts?{filters}&shiftId={shift}";
        var running = (await f.Admin.GetFromJsonAsync<AnalyticsOeeShifts>(url))!;
        var current = Assert.Single(running.Records.Items);
        Assert.True(current.Metrics.PlannedMinutes < 1440);
        Assert.Null(current.Metrics.Performance);
        Assert.Null(current.Metrics.Quality);
        Assert.Contains("run-results-missing", current.Metrics.Missing);
        await End(run, failed: true);

        var measured = (await f.Admin.GetFromJsonAsync<AnalyticsOeeShifts>(url))!;
        var row = Assert.Single(measured.Records.Items);
        Assert.Contains("overlapping-shifts", row.Metrics.Missing);
        Assert.Equal(2, row.Metrics.Processed);
        Assert.Equal(1, row.Metrics.Failed);
        Assert.Equal(0.5, row.Metrics.Quality);
        var shiftMonitor = Assert.Single(row.Evidence, x => x.Kind == "equipment-shift");
        var detail = (await f.Admin.GetFromJsonAsync<WorkbenchShiftMonitor>($"/api/v1/workbench/shift-monitor/{tool}"
            + shiftMonitor.Url[shiftMonitor.Url.IndexOf('?')..]))!;
        Assert.Equal(detail.Metrics!.Quality, row.Metrics.Quality);
        Assert.Equal(detail.Metrics.Performance!.Value, row.Metrics.Performance!.Value, precision: 8);
        var aggregate = (await f.Admin.GetFromJsonAsync<AnalyticsOee>($"/api/v1/analytics/oee?{filters}"))!;
        var device = Assert.Single(aggregate.Devices.Items);
        Assert.Null(device.Metrics.Oee);
        Assert.Contains("overlapping-shifts", device.Metrics.Missing);
        Assert.Equal(HttpStatusCode.BadRequest, (await f.Admin.GetAsync(
            $"/api/v1/analytics/oee?equipmentId={tool}&from=2026-09-01&to=2026-11-01")).StatusCode);
    }
}
