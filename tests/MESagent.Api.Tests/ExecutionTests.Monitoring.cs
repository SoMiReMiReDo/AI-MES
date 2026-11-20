using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using Xunit;

namespace MESagent.Api.Tests;

public sealed partial class ExecutionTests
{
    [Fact]
    public async Task Plant_board_and_shift_monitor_use_live_equipment_and_existing_f10_metrics()
    {
        var sample = await Setup(2, 1);
        var id = Id(sample.Tool);
        var code = sample.Tool.GetProperty("code").GetString();
        var boardPath = $"workbench/plant-board?q={code}";
        var detailPath = $"workbench/shift-monitor/{id}";
        await RegisterFullShift();
        var run = await Start(sample);

        WorkbenchMonitorEquipment OnlyEquipment(WorkbenchPlantBoard? board) => Assert.Single(board!.Equipment.Items);
        var board = await f.Admin.GetFromJsonAsync<WorkbenchPlantBoard>($"/api/v1/{boardPath}");
        Assert.Equal("Running", OnlyEquipment(board).State);
        Assert.Equal(Id(run), OnlyEquipment(board).ActiveRun!.Id);
        Assert.Equal(sample.Order.GetProperty("orderNo").GetString(), OnlyEquipment(board).ActiveRun!.OrderNo);
        Assert.Empty((await Get(boardPath + "&state=Down")).GetProperty("equipment").GetProperty("items").EnumerateArray());

        await End(run, failed: true);
        var tool = (await Get($"equipment/tools/{id}")).GetProperty("equipment");
        await Ok($"equipment/tools/{id}/state", new { state = "Down", reason = "监控验收" }, E(tool));
        board = await f.Admin.GetFromJsonAsync<WorkbenchPlantBoard>($"/api/v1/{boardPath}&state=Down");
        var row = OnlyEquipment(board);
        Assert.Equal("Down", row.State);
        Assert.Equal(1, Assert.Single(board!.Counts).Count);
        Assert.True(row.CurrentStop!.NeedsReason);
        Assert.Null(row.ActiveRun);
        var detail = (await f.Admin.GetFromJsonAsync<WorkbenchShiftMonitor>($"/api/v1/{detailPath}"))!;
        Assert.Equal(row.Shift, detail.Shift);
        Assert.Equal(row.CurrentStop.Id, Assert.Single(detail.Stops).Id);
        Assert.Contains(detail.States, x => x.State == "Down" && x.EndsAt == detail.AsOf);
        Assert.Contains(detail.Runs!, x => x.Id == Id(run));
        Assert.Equal(1, detail.Hours!.Sum(x => x.Passed));
        Assert.Equal(1, detail.Hours!.Sum(x => x.Failed));
        Assert.Null(detail.Metrics!.Oee);
        Assert.Equal("缺少设备理想产速", detail.Metrics.Missing);

        await Ok("workbench/rates", new { equipmentId = id, effectiveOn = detail.Shift!.Day, idealSeconds = 1 });
        var measured = (await f.Admin.GetFromJsonAsync<WorkbenchShiftMonitor>($"/api/v1/{detailPath}"))!;
        var report = (await f.Admin.GetFromJsonAsync<WorkbenchShifts>(
            $"/api/v1/workbench/shifts?equipmentId={id}&from={detail.Shift.Day:yyyy-MM-dd}&to={detail.Shift.Day:yyyy-MM-dd}"))!;
        var equivalent = report.Metrics.Items.First(x => x.StartsAt == measured.Shift!.StartsAt);
        Assert.Equal(0.5, measured.Metrics!.Quality);
        Assert.Equal(equivalent.Quality, measured.Metrics.Quality);
        Assert.Equal(equivalent.Performance, measured.Metrics.Performance);

        var role = "Monitoring-" + Guid.NewGuid().ToString("N")[..12];
        await Ok("security/roles", new { name = role, permissionCodes = new[] { "equipment.read" } });
        using var reader = (await f.NewUser(role)).Client;
        using var unscoped = (await f.NewUser("SystemAdmin", scope: false)).Client;
        var restricted = (await reader.GetFromJsonAsync<WorkbenchShiftMonitor>($"/api/v1/{detailPath}"))!;
        Assert.Null(restricted.Runs);
        Assert.Null(restricted.Hours);
        Assert.Null(restricted.Metrics);
        Assert.Null(restricted.Equipment.ActiveRun);
        Assert.Equal(row.CurrentStop.Id, Assert.Single(restricted.Stops).Id);
        Assert.Null(OnlyEquipment(await reader.GetFromJsonAsync<WorkbenchPlantBoard>($"/api/v1/{boardPath}")).Metrics);
        Assert.Equal(HttpStatusCode.Forbidden, (await unscoped.GetAsync($"/api/v1/{boardPath}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await unscoped.GetAsync($"/api/v1/{detailPath}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await reader.GetAsync($"/api/v1/workbench/shift-monitor/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await reader.GetAsync($"/api/v1/{detailPath}?startsAt=2026-11-19")).StatusCode);
    }

    [Fact]
    public async Task Shift_monitor_selects_overnight_windows_and_clips_actual_state()
    {
        var sample = await Setup(1, 1);
        var id = Id(sample.Tool);
        var date = FabDate().AddDays(-20);
        var start = date.ToDateTime(new TimeOnly(14, 0), DateTimeKind.Utc);
        await Ok("equipment/shifts", new
        {
            code = Guid.NewGuid().ToString("N"), name = "监控跨午夜班次",
            startTime = "22:00:00", endTime = "06:00:00"
        });
        await using (var db = f.Database.CreateContext())
        {
            // Fixture inserts a closed state in the past. The equipment still has its separate current state.
            db.Add(new EquipmentState
            {
                EquipmentId = id, State = "Maintenance", StartedAt = start.AddMinutes(-30),
                EndedAt = start.AddMinutes(60), Source = "Manual"
            });
            await db.SaveChangesAsync();
        }
        var anchor = Uri.EscapeDataString(start.ToString("O", CultureInfo.InvariantCulture));
        var detail = (await f.Admin.GetFromJsonAsync<WorkbenchShiftMonitor>(
            $"/api/v1/workbench/shift-monitor/{id}?startsAt={anchor}"))!;
        Assert.Equal(date, detail.Shift!.Day);
        Assert.Equal(start.AddHours(8), detail.Shift.EndsAt);
        var state = Assert.Single(detail.States);
        Assert.Equal(start, state.StartsAt);
        Assert.Equal(start.AddMinutes(60), state.EndsAt);
        Assert.Equal("Maintenance", state.State);
        Assert.Equal(8, detail.Hours!.Length);
        Assert.All(detail.Hours, x => Assert.Equal(0, x.Passed + x.Failed));
        Assert.True(detail.PreviousShift < detail.Shift.StartsAt);
        Assert.True(detail.NextShift > detail.Shift.StartsAt);
        Assert.Empty(detail.Runs!);
        var local = new DateTimeOffset(start).ToOffset(TimeSpan.FromHours(8))
            .ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
        var byLocalAnchor = await f.Admin.GetFromJsonAsync<WorkbenchShiftMonitor>(
            $"/api/v1/workbench/shift-monitor/{id}?startsAt={Uri.EscapeDataString(local)}");
        Assert.Equal(detail.Shift, byLocalAnchor!.Shift);
    }
}
