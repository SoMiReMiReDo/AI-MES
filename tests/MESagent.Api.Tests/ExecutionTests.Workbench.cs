using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace MESagent.Api.Tests;

public sealed partial class ExecutionTests
{
    private static DateOnly FabDate() => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Asia/Shanghai"));

    private Task<JsonElement> RegisterFullShift() => Ok("equipment/shifts", new
    {
        code = Guid.NewGuid().ToString("N"),
        name = "工作台测试班次",
        startTime = "00:00:00",
        endTime = "23:59:59"
    });

    [Fact]
    public async Task Workbench_overview_and_history_drill_into_the_same_actual_facts()
    {
        var sample = await Setup(3, 1);
        var date = FabDate().ToString("yyyy-MM-dd");
        var operation = sample.Steps[0].GetProperty("operationId").GetGuid();
        var filters = $"operationId={operation}";
        var before = await Get($"workbench/overview?{filters}");
        Assert.Equal(1, before.GetProperty("wipLots").GetInt32());
        Assert.Equal(3, before.GetProperty("wipWafers").GetInt32());
        Assert.Equal(1, before.GetProperty("waiting").GetInt32());
        var waiting = await Get($"workbench/records?kind=waiting&{filters}");
        Assert.Equal(Id(sample.Lot), Id(waiting.GetProperty("items")[0]));

        var run = await Start(sample);
        var active = await Get($"workbench/records?kind=running&{filters}");
        Assert.Equal(Id(run), Id(active.GetProperty("items")[0]));
        await End(run);

        var after = await Get($"workbench/overview?{filters}");
        Assert.Equal(0, after.GetProperty("wipWafers").GetInt32());
        Assert.Equal(3, after.GetProperty("completedToday").GetInt32());
        var history = (await Get($"workbench/history?from={date}&to={date}&{filters}"))[0];
        Assert.Equal(3, history.GetProperty("completed").GetInt32());
        Assert.Equal(3, history.GetProperty("passed").GetInt32());
        Assert.Equal(0, history.GetProperty("failed").GetInt32());
        Assert.True(history.GetProperty("waitMinutes").GetDouble() >= 0);
        Assert.True(history.GetProperty("cycleMinutes").GetDouble() >= 0);
        var shift = await RegisterFullShift();
        var byShift = (await Get($"workbench/history?from={date}&to={date}&shiftId={Id(shift)}&{filters}"))[0];
        Assert.Equal(3, byShift.GetProperty("passed").GetInt32());
        var cycles = await Get($"workbench/records?kind=cycle&date={date}&{filters}");
        Assert.Equal(3, cycles.GetProperty("total").GetInt32());

        var (unscoped, _, _) = await f.NewUser("SystemAdmin", scope: false);
        using (unscoped)
            Assert.Equal(HttpStatusCode.Forbidden, (await unscoped.GetAsync("/api/v1/workbench/overview")).StatusCode);
    }

    [Fact]
    public async Task Workbench_schedule_checks_capacity_maintenance_version_and_real_run_lifecycle()
    {
        var sample = await Setup(4, 1);
        var tool = Id(sample.Tool);
        await RegisterFullShift();
        var date = FabDate().AddDays(1);
        await Ok("workbench/rates", new
        {
            equipmentId = tool, effectiveOn = date, idealSeconds = 600
        });
        var details = await Get($"execution/lots/{Id(sample.Lot)}");
        var child = (await Ok($"production/lots/{Id(sample.Lot)}/split", new
        {
            waferIds = new[] { Id(details.GetProperty("wafers")[0]) },
            reason = "测试不同到访对同一设备排程",
            newLotNumber = Guid.NewGuid().ToString("N")
        }, E(sample.Lot))).GetProperty("newLot");
        var parentStep = Id((await Get($"execution/lots/{Id(sample.Lot)}")).GetProperty("step"));
        var childStep = Id((await Get($"execution/lots/{Id(child)}")).GetProperty("step"));
        var start = date.ToDateTime(new TimeOnly(1, 0), DateTimeKind.Utc);
        var body = new { stepExecutionId = parentStep, equipmentId = tool, startsAt = start, endsAt = start.AddHours(1) };
        var plan = await Ok("workbench/schedule", body);
        Assert.Equal(HttpStatusCode.Conflict, (await Send("workbench/schedule", new
        {
            stepExecutionId = childStep, equipmentId = tool, startsAt = start.AddMinutes(30), endsAt = start.AddHours(2)
        })).StatusCode);

        await Ok("equipment/maintenance", new
        {
            title = "排程维护冲突", eventType = "Preventive", equipmentId = tool, scheduledAt = start.AddHours(3)
        });
        Assert.Equal(HttpStatusCode.Conflict, (await Send($"workbench/schedule/{Id(plan)}", body with
        {
            startsAt = start.AddHours(3), endsAt = start.AddHours(4)
        }, E(plan), method: "PUT")).StatusCode);
        var moved = await Ok($"workbench/schedule/{Id(plan)}", body with
        {
            startsAt = start.AddHours(1), endsAt = start.AddHours(2)
        }, E(plan), "PUT");
        Assert.NotEqual(E(plan), E(moved));
        Assert.Equal(HttpStatusCode.PreconditionFailed, (await Send($"workbench/schedule/{Id(plan)}/cancel", new { },
            E(plan))).StatusCode);

        var key = Guid.NewGuid().ToString();
        var auto = new { from = date, to = date, maxLots = 50 };
        using var automatic = await Send("workbench/schedule/auto", auto, key: key);
        Assert.True(automatic.IsSuccessStatusCode, await automatic.Content.ReadAsStringAsync());
        var created = (await automatic.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("created")
            .EnumerateArray().Single(x => x.GetProperty("lotId").GetGuid() == Id(child));
        Assert.Equal(childStep, created.GetProperty("stepExecutionId").GetGuid());
        using var replay = await Send("workbench/schedule/auto", auto, key: key);
        Assert.True(replay.Headers.Contains("Idempotency-Replayed"));

        await Start(sample);
        var calendar = await Get($"workbench/schedule?equipmentId={tool}&from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}");
        Assert.Equal("Inactive", calendar.GetProperty("plans").EnumerateArray()
            .Single(x => Id(x) == Id(plan)).GetProperty("status").GetString());
        var changes = await Get($"workbench/schedule/{Id(plan)}/history");
        Assert.Equal(new[] { "ScheduleMoved", "ScheduleCreated" },
            changes.EnumerateArray().Select(x => x.GetProperty("action").GetString()));
    }

    [Fact]
    public async Task Workbench_shift_metrics_expose_missing_rate_and_compute_from_actual_run_and_wafer_results()
    {
        var sample = await Setup(2, 1);
        await RegisterFullShift();
        var tool = Id(sample.Tool);
        var date = FabDate();
        var path = $"workbench/shifts?equipmentId={tool}&from={date:yyyy-MM-dd}&to={date:yyyy-MM-dd}";
        var run = await Start(sample);
        await End(run, failed: true);
        JsonElement Metric(JsonElement response) => response.GetProperty("metrics").GetProperty("items")
            .EnumerateArray().Last(x => x.GetProperty("shift").GetString() == "工作台测试班次");
        Assert.Equal(JsonValueKind.Null, Metric(await Get(path)).GetProperty("oee").ValueKind);
        await Ok("workbench/rates", new { equipmentId = tool, effectiveOn = date, idealSeconds = 1 });
        var measured = Metric(await Get(path));
        Assert.Equal(0.5, measured.GetProperty("quality").GetDouble(), precision: 5);
        Assert.True(measured.GetProperty("runMinutes").GetDouble() > 0);
        Assert.True(measured.GetProperty("oee").GetDouble() > 0);
    }

    [Fact]
    public async Task Workbench_alerts_follow_sources_and_isolate_personal_silence_and_escalation()
    {
        var sample = await Setup(1, 1);
        var (recipient, user, _) = await f.NewUser("SystemAdmin");
        using var recipientClient = recipient;
        var target = Id(user);
        Assert.Equal(HttpStatusCode.NoContent, (await Send("workbench/subscriptions/downtime", new
        {
            enabled = true, escalationMinutes = 1, escalateToId = target
        }, method: "PUT")).StatusCode);
        var down = await Ok($"equipment/tools/{Id(sample.Tool)}/state",
            new { state = "Down", reason = "告警端到端测试" }, E(sample.Tool));
        var todo = await Get("workbench/alerts");
        var source = todo.GetProperty("records").GetProperty("items").EnumerateArray().Select(x => x.GetProperty("source"))
            .Single(x => x.GetProperty("kind").GetString() == "downtime"
                && x.GetProperty("equipmentId").GetGuid() == Id(sample.Tool));
        var id = Id(source);
        const string filter = "workbench/alerts?mode=notifications&kind=downtime";
        async Task<JsonElement?> Notice(HttpClient client)
        {
            var data = await client.GetFromJsonAsync<JsonElement>("/api/v1/" + filter);
            return data.GetProperty("records").GetProperty("items").EnumerateArray()
                .Where(x => Id(x.GetProperty("source")) == id).Select(x => (JsonElement?)x).SingleOrDefault();
        }
        Assert.NotNull(await Notice(f.Admin));
        Assert.Null(await Notice(recipient));

        try
        {
            f.Clock.Offset = TimeSpan.FromMinutes(2);
            Assert.True((await Notice(recipient))!.Value.GetProperty("escalated").GetBoolean());
            Assert.Equal(HttpStatusCode.NoContent, (await Send($"workbench/alerts/downtime/{id}",
                new { action = "silence", until = DateTime.UtcNow.AddMinutes(5) }, method: "PUT")).StatusCode);
            Assert.Null(await Notice(f.Admin));
            Assert.Null(await Notice(recipient));
            Assert.Equal(HttpStatusCode.NoContent, (await Send($"workbench/alerts/downtime/{id}",
                new { action = "acknowledge" }, method: "PUT")).StatusCode);
            Assert.Equal(JsonValueKind.String,
                (await Get(filter + "&includeSilenced=true")).GetProperty("records").GetProperty("items")
                    .EnumerateArray().Single(x => Id(x.GetProperty("source")) == id)
                    .GetProperty("acknowledgedAt").ValueKind);
            await Ok($"equipment/tools/{Id(sample.Tool)}/state", new { state = "Idle", reason = "恢复设备" }, E(down));
        }
        finally
        {
            f.Clock.Offset = TimeSpan.Zero;
        }
        Assert.Null(await Notice(f.Admin));
    }
}
