using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.WorkbenchDashboardsAlertsScheduling;

internal sealed partial class WorkbenchBackend
{
    private sealed record ShiftWindow(string Label, DateOnly Day, DateTime From, DateTime To);

    private static ShiftWindow[] Windows(Equipment tool, Shift[] shifts, IEnumerable<DateOnly> days, TimeZoneInfo zone)
    {
        return (from shift in shifts
                where shift.LineId == null || shift.LineId == tool.LineId
                let weekdays = ShiftDays(shift)
                from day in days
                where weekdays.Contains(Weekday(day))
                let start = Utc(day, shift.StartTime, zone)
                let to = Utc(shift.EndTime < shift.StartTime ? day.AddDays(1) : day, shift.EndTime, zone)
                where to > start
                orderby start, to, shift.Code, shift.Id
                select new ShiftWindow(shift.Name, day, start, to)).ToArray();
    }

    private static int Weekday(DateOnly day) => ((int)day.DayOfWeek + 6) % 7 + 1;

    private static int[] ShiftDays(Shift shift) => JsonSerializer.Deserialize<int[]>(shift.DaysOfWeek) ?? [];

    private Task<Shift[]> ShiftDefinitions() => db.Set<Shift>().AsNoTracking().Where(x => x.FabId == fabId && x.IsActive
        && (x.LineId == null || (from line in db.Set<Line>()
                                 join area in db.Set<Area>() on line.AreaId equals area.Id
                                 where line.Id == x.LineId && area.FabId == fabId && line.IsActive && area.IsActive
                                 select line.Id).Any())).ToArrayAsync();

    private IQueryable<EquipmentIdealRate> Rates => db.Set<EquipmentIdealRate>().AsNoTracking()
        .Where(x => x.FabId == fabId);

    private static decimal? EffectiveRate(IEnumerable<EquipmentIdealRate> rates, Guid toolId, DateOnly on) =>
        rates.Where(x => x.EquipmentId == toolId && x.EffectiveOn <= on).MaxBy(x => x.EffectiveOn)?.IdealSeconds;

    private async Task<OrganizationIdentityAccessControlResult> AddRate(AddWorkbenchRate input)
    {
        require(WorkbenchAccess.Equipment);
        require(WorkbenchAccess.ConfigureRate);
        await RequireActiveFab();
        var today = Today(await Zone());
        if (input.IdealSeconds <= 0 || input.IdealSeconds > 86400
            || decimal.Round(input.IdealSeconds, 3) != input.IdealSeconds
            || input.EffectiveOn.Year is < 1900 or > 2200)
            throw Invalid("理想秒/片须大于 0 且不超过 86400，最多三位小数，并填写生效日期");
        if (!await Equipment.AnyAsync(x => x.Id == input.EquipmentId))
            throw Missing();
        var existing = await Rates.SingleOrDefaultAsync(x => x.EquipmentId == input.EquipmentId
            && x.EffectiveOn == input.EffectiveOn);
        if (existing != null)
        {
            if (existing.IdealSeconds == input.IdealSeconds)
                return new(input, Replayed: true);
            throw Conflict("该设备在这一生效日已有产速记录");
        }
        if (input.EffectiveOn < today && await Rates.AnyAsync(x => x.EquipmentId == input.EquipmentId))
            throw Invalid("已有产速的设备须从今天或未来日期配置新产速");
        var rate = new EquipmentIdealRate
        {
            FabId = fabId, EquipmentId = input.EquipmentId,
            EffectiveOn = input.EffectiveOn, IdealSeconds = input.IdealSeconds
        };
        db.Add(rate);
        Audit(rate.Id, nameof(EquipmentIdealRate), "RateAdded", null, input);
        return new(input, Status: 201);
    }

    private async Task<WorkbenchShifts> Shifts(IReadOnlyDictionary<string, string> q)
    {
        require(WorkbenchAccess.Execution);
        require(WorkbenchAccess.Equipment);
        var zone = await Zone();
        var today = Today(zone);
        var days = Days(Date(q, "from", today), Date(q, "to", today), 7);
        var shifts = await ShiftDefinitions();
        var equipment = await FilterEquipment(q).Where(x => x.IsActive).OrderBy(x => x.Code)
            .ThenBy(x => x.Id).ToArrayAsync();
        var page = Number(q, "page", 1, 1000000);
        var size = Number(q, "pageSize", 20, 100);
        var windows = equipment.SelectMany(tool =>
        {
            var found = Windows(tool, shifts, days, zone);
            return found.Length == 0 ? [(Tool: tool, Window: (ShiftWindow?)null)]
                : found.Select(window => (Tool: tool, Window: (ShiftWindow?)window));
        }).ToArray();
        var selected = windows.Skip((page - 1) * size).Take(size).ToArray();
        var toolIds = selected.Select(x => x.Tool.Id).Distinct().ToArray();
        var lower = Utc(days[0], TimeOnly.MinValue, zone);
        var upper = Utc(days[^1].AddDays(2), TimeOnly.MinValue, zone);
        var runs = await (from run in HistoryRuns(q)
                          where toolIds.Contains(run.Run.EquipmentId) && run.Run.TrackInAt < upper
                              && (run.Run.TrackOutAt == null || run.Run.TrackOutAt > lower)
                          select new
                          {
                              run.Run.Id, run.Run.EquipmentId, run.Run.TrackInAt, run.Run.TrackOutAt, run.Run.Status,
                              Passed = db.Set<ProcessRunWafer>().Count(x => x.ProcessRunId == run.Run.Id
                                  && x.Result == "Passed"),
                              Failed = db.Set<ProcessRunWafer>().Count(x => x.ProcessRunId == run.Run.Id
                                  && x.Result == "Failed"),
                              Members = db.Set<ProcessRunWafer>().Count(x => x.ProcessRunId == run.Run.Id)
                          }).ToArrayAsync();
        var losses = await db.Set<Downtime>().Where(x => toolIds.Contains(x.EquipmentId)
            && x.StartedAt < upper && (x.EndedAt == null || x.EndedAt > lower))
            .Select(x => new { x.EquipmentId, x.StartedAt, x.EndedAt }).ToArrayAsync();
        var lastDay = days[^1];
        var rates = await Rates.Where(x => toolIds.Contains(x.EquipmentId) && x.EffectiveOn <= lastDay)
            .ToArrayAsync();
        var metrics = new List<WorkbenchShiftMetric>();
        foreach (var (tool, window) in selected)
        {
            if (window == null || window.From >= Now)
            {
                metrics.Add(new(tool.Id, tool.Code, window?.Label, window?.From, window?.To,
                    null, null, null, null, null, null, null, null, null, null, null,
                    window == null ? "无适用班次" : "班次尚未开始"));
                continue;
            }

            var end = Min(Now, window.To);
            var planned = (end - window.From).TotalMinutes;
            var overlaps = runs.Where(x => x.EquipmentId == tool.Id && x.TrackInAt < end
                && (x.TrackOutAt == null || x.TrackOutAt > window.From)).ToArray();
            var running = Coverage(overlaps.Select(x => (x.TrackInAt, x.TrackOutAt ?? end)), window.From, end);
            var stopped = Coverage(losses.Where(x => x.EquipmentId == tool.Id)
                .Select(x => (x.StartedAt, x.EndedAt ?? end)), window.From, end);
            var rate = EffectiveRate(rates, tool.Id, window.Day);
            var classified = overlaps.Length > 0 && overlaps.All(x => x.Status == "Completed"
                && x.TrackOutAt > x.TrackInAt && x.Members > 0 && x.Members == x.Passed + x.Failed);
            double? processed = classified ? overlaps.Sum(x => x.Members
                * Share(x.TrackInAt, x.TrackOutAt!.Value, window.From, end)) : null;
            double? failed = classified ? overlaps.Sum(x => x.Failed
                * Share(x.TrackInAt, x.TrackOutAt!.Value, window.From, end)) : null;
            var availability = running / planned;
            var ideal = (double?)rate * processed / 60;
            var performance = running > 0 ? ideal / running : null;
            var quality = processed > 0 ? (processed - failed) / processed : null;
            metrics.Add(new(tool.Id, tool.Code, window.Label, window.From, window.To,
                planned, running, stopped, rate, availability, performance, quality,
                availability * performance * quality, planned - running,
                ideal == null ? null : Math.Max(0, running - ideal.Value), failed,
                rate == null ? "缺少设备理想产速" : !classified ? "缺少完整逐片结果" : null));
        }

        var queue = FilterWip(q).Where(x => x.Step.Status == "Waiting");
        var bottlenecks = await (from row in queue
                                 join operation in db.Set<Operation>() on row.Route.OperationId equals operation.Id
                                 group row by new { operation.Id, operation.Name } into g
                                 orderby g.Count() descending, g.Min(x => x.Step.EnteredAt), g.Key.Id
                                 select new WorkbenchBottleneck(
                                     g.Key.Id, g.Key.Name, g.Count(), g.Min(x => x.Step.EnteredAt)))
            .Take(10).ToArrayAsync();
        return new(new(metrics.ToArray(), windows.Length, page, size), bottlenecks);
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

    private static double Share(DateTime start, DateTime stop, DateTime from, DateTime to) =>
        (Min(stop, to) - Max(start, from)).TotalSeconds / (stop - start).TotalSeconds;

    private static double Coverage(IEnumerable<(DateTime From, DateTime To)> intervals, DateTime from, DateTime to)
    {
        var end = from;
        var minutes = 0d;
        foreach (var interval in intervals.OrderBy(x => x.From))
        {
            var left = Max(end, interval.From);
            var right = Min(to, interval.To);
            if (left >= right)
                continue;
            minutes += (right - left).TotalMinutes;
            end = right;
        }
        return minutes;
    }
}
