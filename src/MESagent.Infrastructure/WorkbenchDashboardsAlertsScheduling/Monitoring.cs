using System.Globalization;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.WorkbenchDashboardsAlertsScheduling;

internal sealed partial class WorkbenchBackend
{
    private static readonly IReadOnlyDictionary<string, string> NoFilters = new Dictionary<string, string>();

    private sealed class MonitorTool
    {
        public required Equipment Tool { get; init; }
        public required string Area { get; init; }
        public string? Line { get; init; }
        public required string State { get; init; }
        public DateTime? StateSince { get; init; }
    }

    private IQueryable<MonitorTool> MonitorTools(IQueryable<Equipment> equipment) =>
        from tool in equipment
        join area in db.Set<Area>() on tool.AreaId equals area.Id
        let state = db.Set<EquipmentState>()
            .Where(x => x.EquipmentId == tool.Id && x.StartedAt <= Now && (x.EndedAt == null || x.EndedAt > Now))
            .OrderByDescending(x => x.StartedAt).ThenBy(x => x.Id).FirstOrDefault()
        where tool.IsActive
        select new MonitorTool
        {
            Tool = tool,
            Area = area.Name,
            Line = db.Set<Line>().Where(x => x.Id == tool.LineId && x.AreaId == tool.AreaId)
                .Select(x => x.Name).FirstOrDefault(),
            State = state == null ? "Unknown" : state.State,
            StateSince = state == null ? null : state.StartedAt
        };

    private IQueryable<WorkbenchMonitorStop> MonitorStops(Guid[] tools, DateTime from, DateTime to) =>
        db.Set<Downtime>().Where(x => tools.Contains(x.EquipmentId)
            && x.StartedAt < to && (x.EndedAt == null || x.EndedAt > from))
            .OrderBy(x => x.StartedAt).ThenBy(x => x.Id)
            .Select(x => new WorkbenchMonitorStop(x.Id, x.EquipmentId, x.StartedAt, x.EndedAt,
                db.Set<ReasonCode>().Where(r => r.Id == x.ReasonCodeId).Select(r => r.Name).FirstOrDefault(),
                x.NeedsReason || x.ReasonCodeId == null, x.Notes));

    private IQueryable<WorkbenchMonitorRun> MonitorRuns(Guid[] tools, DateTime lower, DateTime to, bool active = false) =>
        from row in HistoryRuns(NoFilters)
        where tools.Contains(row.Run.EquipmentId) && row.Run.TrackInAt < to
            && (row.Run.TrackOutAt == null || row.Run.TrackOutAt > lower)
            && (!active || row.Run.Status == "Running" && row.Run.TrackOutAt == null)
        orderby row.Run.TrackInAt descending, row.Run.Id
        select new WorkbenchMonitorRun(row.Run.Id, row.Run.EquipmentId, row.Lot.LotNumber,
            db.Set<WorkOrder>().Where(x => x.Id == row.Lot.WorkOrderId && x.FabId == fabId)
                .Select(x => x.OrderNo).FirstOrDefault(),
            row.Step.Name, row.Run.Status, row.Run.TrackInAt, row.Run.TrackOutAt);

    private static ShiftWindow[] MonitorWindows(Equipment tool, Shift[] definitions,
        IEnumerable<DateOnly> days, TimeZoneInfo zone) =>
        Windows(tool, definitions, days, zone).GroupBy(x => x.From)
            .Select(group => group.OrderByDescending(x => x.ForLine).First()).ToArray();

    private static ShiftWindow? ActiveWindow(IEnumerable<ShiftWindow> windows, DateTime at) =>
        windows.Where(x => x.From <= at && at < x.To)
            .OrderByDescending(x => x.ForLine).ThenByDescending(x => x.From).FirstOrDefault();

    private static WorkbenchMonitorWindow? WindowView(ShiftWindow? window) =>
        window == null ? null : new(window.Label, window.Day, window.From, window.To);

    private static WorkbenchMonitorEquipment MonitorView(MonitorTool row, ShiftWindow? window,
        WorkbenchMonitorStop? stop, WorkbenchMonitorRun? active, WorkbenchShiftMetric? metrics = null) =>
        new(row.Tool.Id, row.Tool.Code, row.Tool.Name, row.Area, row.Line, row.State, row.StateSince,
            row.State == "Down" ? stop : null, active, WindowView(window), metrics);

    private async Task<WorkbenchPlantBoard> PlantBoard(IReadOnlyDictionary<string, string> query)
    {
        require(WorkbenchAccess.Equipment);
        var area = FilterId(query, "areaId");
        var keyword = query.GetValueOrDefault("q", "").Trim().ToUpperInvariant();
        if (keyword.Length > 100)
            throw Invalid("设备关键词最多 100 字");
        var state = query.GetValueOrDefault("state", "");
        if (state is not ("" or "Running" or "Idle" or "Down" or "Maintenance" or "Offline" or "Unknown"))
            throw Invalid("无效设备状态");

        var areas = await db.Set<Area>().Where(x => x.FabId == fabId).OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArrayAsync();
        var equipment = MonitorTools(Equipment.Where(x => (area == null || x.AreaId == area)
            && (keyword == "" || x.Code.ToUpper().Contains(keyword) || x.Name.ToUpper().Contains(keyword))));
        var counts = await equipment.GroupBy(x => x.State).OrderBy(x => x.Key)
            .Select(x => new WorkbenchMonitorCount(x.Key, x.Count())).ToArrayAsync();
        var page = await Page(equipment.Where(x => state == "" || x.State == state)
            .OrderBy(x => x.State == "Down" ? 0 : x.State == "Maintenance" ? 1 : 2)
            .ThenBy(x => x.Tool.Code).ThenBy(x => x.Tool.Id), query);
        if (page.Items.Length == 0)
            return new(Now, areas, counts, new([], page.Total, page.Page, page.PageSize));

        var zone = await Zone();
        var today = Today(zone);
        var shifts = await ShiftDefinitions();
        var windows = page.Items.ToDictionary(x => x.Tool.Id,
            x => ActiveWindow(MonitorWindows(x.Tool, shifts, [today.AddDays(-1), today], zone), Now));
        var tools = page.Items.Select(x => x.Tool.Id).ToArray();
        var downTools = page.Items.Where(x => x.State == "Down").Select(x => x.Tool.Id).ToArray();
        var stops = (await MonitorStops(downTools, Now, Now).ToArrayAsync()).ToLookup(x => x.EquipmentId);

        var showRuns = can(WorkbenchAccess.Execution);
        var active = showRuns ? await MonitorRuns(tools, Now, Now, active: true).ToArrayAsync() : [];
        var activeByTool = active.ToLookup(x => x.EquipmentId);
        var programmed = windows.Where(x => x.Value != null).Select(x => x.Key).ToArray();
        var start = windows.Values.Where(x => x != null).Select(x => x!.From).DefaultIfEmpty(Now).Min();
        var runs = (showRuns && programmed.Length > 0 ? await ShiftRuns(NoFilters, programmed, start, Now) : [])
            .ToLookup(x => x.EquipmentId);
        var losses = (showRuns && programmed.Length > 0 ? await ShiftLosses(programmed, start, Now) : [])
            .ToLookup(x => x.EquipmentId);
        var rates = (showRuns && programmed.Length > 0
            ? await Rates.Where(x => programmed.Contains(x.EquipmentId) && x.EffectiveOn <= today)
                .ToArrayAsync() : []).ToLookup(x => x.EquipmentId);

        var items = page.Items.Select(row =>
        {
            var id = row.Tool.Id;
            var window = windows[id];
            var metrics = showRuns && window != null
                ? ShiftMetrics(row.Tool, window, runs[id], losses[id], rates[id]) : null;
            return MonitorView(row, window, stops[id].LastOrDefault(), activeByTool[id].FirstOrDefault(), metrics);
        }).ToArray();
        return new(Now, areas, counts, new(items, page.Total, page.Page, page.PageSize));
    }

    private static DateTime? MonitorAnchor(IReadOnlyDictionary<string, string> query)
    {
        if (!query.TryGetValue("startsAt", out var value))
            return null;
        if (!DateTime.TryParseExact(value, ["yyyy-MM-dd'T'HH:mm:ssK", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK"],
                CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
            || date.Kind == DateTimeKind.Unspecified || date.Year is < 1900 or > 2200)
            throw Invalid("班次开始时间须为带时区的 ISO 8601 时间");
        return date.ToUniversalTime();
    }

    private async Task<WorkbenchShiftMonitor> ShiftMonitor(Guid id, IReadOnlyDictionary<string, string> query)
    {
        require(WorkbenchAccess.Equipment);
        var row = await MonitorTools(Equipment.Where(x => x.Id == id)).SingleOrDefaultAsync() ?? throw Missing();
        var anchor = MonitorAnchor(query);
        var shiftId = FilterId(query, "shiftId");
        if (shiftId != null && anchor == null)
            throw Invalid("指定班次时须提供班次开始时间");
        var zone = await Zone();
        var today = Today(zone);
        var date = anchor is DateTime value ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(value, zone))
            : today;
        var definitions = await ShiftDefinitions();
        var days = Enumerable.Range(-14, 29).Select(date.AddDays);
        var windows = MonitorWindows(row.Tool, definitions, days, zone);
        var window = anchor == null ? ActiveWindow(windows, Now)
            : (shiftId == null ? windows : Windows(row.Tool, definitions, [date], zone))
                .FirstOrDefault(x => x.From == anchor && (shiftId == null || x.Id == shiftId)) ?? throw Missing();
        var pivot = window?.From ?? Now;
        var previous = windows.LastOrDefault(x => x.From < pivot)?.From;
        var next = windows.FirstOrDefault(x => x.From > pivot)?.From;
        var devices = await Equipment.Where(x => x.IsActive).OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArrayAsync();
        var showRuns = can(WorkbenchAccess.Execution);
        var tools = new[] { id };
        var active = showRuns
            ? await MonitorRuns(tools, Now, Now, active: true).FirstOrDefaultAsync() : null;
        var currentStop = row.State == "Down"
            ? await MonitorStops(tools, Now, Now).LastOrDefaultAsync() : null;
        var currentWindow = anchor == null ? window
            : ActiveWindow(MonitorWindows(row.Tool, definitions, [today.AddDays(-1), today], zone), Now);
        var current = MonitorView(row, currentWindow, currentStop, active);
        var end = window == null ? Now : Min(window.To, Now);

        if (window == null || window.From >= end)
            return new(Now, current, devices, WindowView(window), previous, next, null, [], [],
                showRuns ? [] : null, showRuns ? [] : null);

        var from = window.From;
        var intervals = await db.Set<EquipmentState>().Where(x => x.EquipmentId == id
            && x.StartedAt < end && (x.EndedAt == null || x.EndedAt > from))
            .OrderBy(x => x.StartedAt).ThenBy(x => x.Id)
            .Select(x => new { x.State, x.StartedAt, x.EndedAt }).ToArrayAsync();
        var states = intervals.Select(x =>
            new WorkbenchMonitorState(x.State, Max(x.StartedAt, from), Min(x.EndedAt ?? end, end))).ToArray();
        var stops = await MonitorStops(tools, from, end).ToArrayAsync();
        WorkbenchShiftMetric? metrics = null;
        WorkbenchMonitorRun[]? runs = null;
        WorkbenchMonitorHour[]? hours = null;
        if (showRuns)
        {
            runs = await MonitorRuns(tools, from, end).ToArrayAsync();
            hours = await MonitorHours(id, from, end);
            var rates = await Rates.Where(x => x.EquipmentId == id && x.EffectiveOn <= window.Day)
                .OrderByDescending(x => x.EffectiveOn).Take(1).ToArrayAsync();
            metrics = ShiftMetrics(row.Tool, window, await ShiftRuns(NoFilters, tools, from, end),
                stops.Select(x => new ShiftLoss(x.Id, id, x.StartsAt, x.EndsAt)), rates);
        }
        return new(Now, current, devices, WindowView(window), previous, next, metrics, states, stops, runs, hours);
    }

    private async Task<WorkbenchMonitorHour[]> MonitorHours(Guid id, DateTime lower, DateTime end)
    {
        var results = await (from run in HistoryRuns(NoFilters)
                             join wafer in db.Set<ProcessRunWafer>() on run.Run.Id equals wafer.ProcessRunId
                             where run.Run.EquipmentId == id && wafer.ProcessedAt >= lower && wafer.ProcessedAt < end
                                 && (wafer.Result == "Passed" || wafer.Result == "Failed")
                             group wafer by Math.Floor((wafer.ProcessedAt!.Value - lower).TotalHours) into bucket
                             select new
                             {
                                 Hour = bucket.Key,
                                 Passed = bucket.Count(x => x.Result == "Passed"),
                                 Failed = bucket.Count(x => x.Result == "Failed")
                             }).ToDictionaryAsync(x => (int)x.Hour);
        return Enumerable.Range(0, (int)Math.Ceiling((end - lower).TotalHours)).Select(i =>
        {
            var start = lower.AddHours(i);
            var bucket = results.GetValueOrDefault(i);
            return new WorkbenchMonitorHour(start, Min(start.AddHours(1), end),
                bucket?.Passed ?? 0, bucket?.Failed ?? 0);
        }).ToArray();
    }
}
