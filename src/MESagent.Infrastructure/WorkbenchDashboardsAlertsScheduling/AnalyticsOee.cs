using System.Globalization;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.ProductionAnalytics;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.WorkbenchDashboardsAlertsScheduling;

internal sealed partial class WorkbenchBackend
{
    private const string OeeMetricVersion = "equipment-shift-oee-v1";

    private static readonly IReadOnlyDictionary<string, string> OeeUnits = new Dictionary<string, string>
    {
        ["duration"] = "minute", ["idealSeconds"] = "second/wafer-pass",
        ["quantity"] = "wafer-pass", ["ratio"] = "fraction"
    };

    private sealed record OeeFilter(AnalyticsWindow Dates, Guid? Area, Guid? Line,
        Guid? Equipment, Guid? Shift, int Page, int Size)
    {
        public Dictionary<string, string> Values => AnalyticsBackend.Filters(Dates,
            ("areaId", Area), ("lineId", Line), ("equipmentId", Equipment), ("shiftId", Shift));
    }

    private sealed record OeeWindow(Equipment Device, ShiftWindow? Window, bool Overlap);

    private sealed record OeeSample(AnalyticsOeeShift Shift, bool Overlap);

    private static OeeFilter OeeFilters(AnalyticsWindow dates, IReadOnlyDictionary<string, string> query)
    {
        AnalyticsBackend.Validate(query,
            "from", "to", "areaId", "lineId", "equipmentId", "shiftId", "page", "pageSize");
        var (page, size) = AnalyticsBackend.Paging(query);
        return new(dates, AnalyticsBackend.Filter(query, "areaId"), AnalyticsBackend.Filter(query, "lineId"),
            AnalyticsBackend.Filter(query, "equipmentId"), AnalyticsBackend.Filter(query, "shiftId"), page, size);
    }

    private IQueryable<Equipment> OeeDevices(OeeFilter filter, Shift[] definitions)
    {
        var candidates = Equipment.Where(x => x.IsActive && (filter.Area == null || x.AreaId == filter.Area)
            && (filter.Line == null || x.LineId == filter.Line)
            && (filter.Equipment == null || x.Id == filter.Equipment));
        if (filter.Shift is Guid shiftId)
        {
            var shift = definitions.SingleOrDefault(x => x.Id == shiftId);
            if (shift == null)
                return candidates.Where(_ => false);
            if (shift.LineId is Guid line)
                return candidates.Where(x => x.LineId == line);
        }
        return candidates;
    }

    private IEnumerable<OeeWindow> OeeWindows(Equipment[] devices, Shift[] definitions, OeeFilter filter)
    {
        var dates = filter.Dates;
        var days = new[] { dates.From.AddDays(-1) }.Concat(dates.Days).Append(dates.To.AddDays(1));
        foreach (var device in devices)
        {
            var all = Windows(device, definitions, days, dates.Zone);
            var selected = all.Where(x => x.Day >= dates.From && x.Day <= dates.To
                && (filter.Shift == null || x.Id == filter.Shift)).ToArray();
            if (selected.Length == 0)
                yield return new(device, null, false);
            foreach (var window in selected)
            {
                var overlap = all.Any(other => other != window
                    && Max(other.From, window.From) < Min(Now, Min(other.To, window.To)));
                yield return new(device, window, overlap);
            }
        }
    }

    private async Task<OeeSample[]> Observe(OeeWindow[] selected, AnalyticsWindow dates)
    {
        var windows = selected.Where(x => x.Window != null && x.Window.From < Now).ToArray();
        var devices = windows.Select(x => x.Device.Id).Distinct().ToArray();
        var lower = windows.Length > 0 ? windows.Min(x => x.Window!.From) : Now;
        var upper = windows.Length > 0 ? Min(Now, windows.Max(x => x.Window!.To)) : Now;
        var runs = devices.Length > 0
            ? (await ShiftRuns(NoFilters, devices, lower, upper)).ToLookup(x => x.EquipmentId) : null;
        var losses = devices.Length > 0
            ? (await ShiftLosses(devices, lower, upper)).ToLookup(x => x.EquipmentId) : null;
        var rates = devices.Length > 0 ? (await Rates.Where(x =>
            devices.Contains(x.EquipmentId) && x.EffectiveOn <= dates.To).ToArrayAsync()).ToLookup(x => x.EquipmentId)
            : null;

        return selected.Select(row =>
        {
            var (device, window, overlap) = row;
            var toolRuns = runs?[device.Id] ?? [];
            var toolLosses = losses?[device.Id] ?? [];
            var observation = ObserveShift(device, window, toolRuns, toolLosses, rates?[device.Id] ?? []);
            var m = observation.Metric;
            var metrics = new AnalyticsOeeMetrics(m.PlannedMinutes, m.RunMinutes, m.DowntimeMinutes,
                observation.IdealMinutes, observation.Processed, observation.Processed - observation.Failed,
                observation.Failed, m.Availability, m.Performance, m.Quality, m.Oee,
                overlap ? [.. observation.Missing, "overlapping-shifts"] : observation.Missing);
            var evidence = window == null ? Array.Empty<AnalyticsEvidence>() :
                ShiftEvidence(device.Id, window, toolRuns, toolLosses);
            return new OeeSample(new(device.Id, device.Code, window?.Id, m.Shift, window?.Day,
                m.StartsAt, m.EndsAt, m.IdealSeconds, metrics, evidence), overlap);
        }).ToArray();
    }

    private AnalyticsEvidence[] ShiftEvidence(Guid toolId, ShiftWindow window,
        IEnumerable<ShiftRun> runs, IEnumerable<ShiftLoss> losses)
    {
        var at = Uri.EscapeDataString(window.From.ToString("O", CultureInfo.InvariantCulture));
        var end = Min(window.To, Now);
        var evidence = new List<AnalyticsEvidence>
        {
            new("equipment-shift", window.Id, window.From, $"/monitoring/{toolId}?startsAt={at}&shiftId={window.Id}")
        };
        if (window.From >= end)
            return evidence.ToArray();
        evidence.AddRange(losses.Where(x => x.StartedAt < end && (x.EndedAt == null || x.EndedAt > window.From))
            .OrderByDescending(x => x.StartedAt).ThenBy(x => x.Id).Take(2)
            .Select(x => new AnalyticsEvidence("downtime", x.Id, x.StartedAt,
                $"/equipment?view=downtimes&record={x.Id}")));
        evidence.AddRange(runs.Where(x => x.TrackInAt < end && (x.TrackOutAt == null || x.TrackOutAt > window.From))
            .OrderByDescending(x => x.TrackInAt).ThenBy(x => x.Id).Take(2)
            .Select(x => new AnalyticsEvidence("process-run", x.Id, x.TrackInAt, $"/execution?run={x.Id}")));
        return evidence.ToArray();
    }

    private static AnalyticsOeeMetrics Aggregate(IEnumerable<OeeSample> samples)
    {
        var rows = samples.ToArray();
        var elapsed = rows.Where(x => x.Shift.Metrics.PlannedMinutes != null).ToArray();
        if (elapsed.Length == 0 || elapsed.Any(x => x.Overlap))
            return new(null, null, null, null, null, null, null, null, null, null, null, rows.Length == 0
                ? ["no-shift"] : rows.SelectMany(x => x.Shift.Metrics.Missing).Distinct().Order().ToArray());

        var metrics = elapsed.Select(x => x.Shift.Metrics).ToArray();
        var planned = metrics.Sum(x => x.PlannedMinutes!.Value);
        var running = metrics.Sum(x => x.RunMinutes!.Value);
        var downtime = metrics.Sum(x => x.DowntimeMinutes!.Value);
        var productive = metrics.Where(x => x.RunMinutes > 0).ToArray();
        var knownResults = metrics.All(x => !x.Missing.Contains("run-results-missing"));
        var knownRate = knownResults && productive.All(x => x.IdealMinutes.HasValue);
        double? processed = knownResults ? productive.Sum(x => x.Processed!.Value) : null;
        double? failed = knownResults ? productive.Sum(x => x.Failed!.Value) : null;
        double? ideal = knownRate ? productive.Sum(x => x.IdealMinutes!.Value) : null;
        var availability = AnalyticsBackend.Ratio(running, planned);
        var performance = ideal != null ? AnalyticsBackend.Ratio(ideal.Value, running) : null;
        var quality = processed > 0 && failed != null ? (processed - failed) / processed : null;
        var missing = rows.SelectMany(x => x.Shift.Metrics.Missing).Distinct().Order().ToArray();
        return new(planned, running, downtime, ideal, processed, processed - failed, failed,
            availability, performance, quality, availability * performance * quality, missing);
    }

    internal async Task<AnalyticsOee> AnalyticsOee(AnalyticsWindow dates, IReadOnlyDictionary<string, string> query)
    {
        var filter = OeeFilters(dates, query);
        var definitions = await ShiftDefinitions();
        var devices = OeeDevices(filter, definitions);
        var total = await devices.CountAsync();
        var selected = await devices.OrderBy(x => x.Code).ThenBy(x => x.Id).Skip((filter.Page - 1) * filter.Size)
            .Take(filter.Size).ToArrayAsync();
        var rows = await Observe(OeeWindows(selected, definitions, filter).ToArray(), dates);
        var byDevice = rows.ToLookup(x => x.Shift.EquipmentId);
        var items = selected.Select(device =>
        {
            var samples = byDevice[device.Id].ToArray();
            var byDay = samples.ToLookup(x => x.Shift.Date);
            return new AnalyticsOeeDevice(device.Id, device.Code, device.Name, device.AreaId, device.LineId,
                samples.Count(x => x.Shift.Metrics.PlannedMinutes != null),
                samples.Count(x => x.Shift.Metrics.Oee != null),
                Aggregate(samples),
                dates.Days.Select(day => new AnalyticsOeePoint(day, Aggregate(byDay[day]))).ToArray());
        }).ToArray();
        var areas = await db.Set<Area>().Where(x => x.FabId == fabId && x.IsActive)
            .OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArrayAsync();
        var lines = await (from line in db.Set<Line>()
                           join area in db.Set<Area>() on line.AreaId equals area.Id
                           where area.FabId == fabId && area.IsActive && line.IsActive
                               && (filter.Area == null || area.Id == filter.Area)
                           orderby line.Code, line.Id
                           select new WorkbenchOption(line.Id, line.Code, line.Name)).ToArrayAsync();
        var options = await OeeDevices(filter with { Equipment = null, Shift = null }, definitions)
            .OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArrayAsync();
        var shifts = definitions.OrderBy(x => x.SortOrder).ThenBy(x => x.Code, StringComparer.Ordinal).ThenBy(x => x.Id)
            .Select(x => new WorkbenchOption(x.Id, x.Code, x.Name)).ToArray();
        return new(OeeContext(filter, rows), new(items, total, filter.Page, filter.Size),
            areas, lines, options, shifts);
    }

    internal async Task<AnalyticsOeeShifts> AnalyticsShiftRows(
        AnalyticsWindow dates, IReadOnlyDictionary<string, string> query)
    {
        var filter = OeeFilters(dates, query);
        var definitions = await ShiftDefinitions();
        var devices = await OeeDevices(filter, definitions).OrderBy(x => x.Code).ThenBy(x => x.Id).ToArrayAsync();
        var all = OeeWindows(devices, definitions, filter).OrderByDescending(x => x.Window?.From)
            .ThenBy(x => x.Device.Code, StringComparer.Ordinal).ThenBy(x => x.Device.Id).ThenBy(x => x.Window?.Id)
            .ToArray();
        var rows = await Observe(all.Skip((filter.Page - 1) * filter.Size).Take(filter.Size).ToArray(), dates);
        return new(OeeContext(filter, rows),
            new(rows.Select(x => x.Shift).ToArray(), all.Length, filter.Page, filter.Size));
    }

    private AnalyticsContext OeeContext(OeeFilter filter, OeeSample[] rows) =>
        AnalyticsBackend.Context(Now, filter.Dates.Zone.Id, OeeMetricVersion, filter.Values, OeeUnits,
            rows.SelectMany(x => x.Shift.Metrics.Missing),
            rows.OrderByDescending(x => x.Shift.StartsAt).SelectMany(x => x.Shift.Evidence));
}
