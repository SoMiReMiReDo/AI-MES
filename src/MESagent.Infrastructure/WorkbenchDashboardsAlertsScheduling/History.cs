using System.Linq.Expressions;
using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.WorkbenchDashboardsAlertsScheduling;

internal sealed partial class WorkbenchBackend
{
    private IQueryable<Lot> HistoryLots(IReadOnlyDictionary<string, string> q)
    {
        var product = FilterId(q, "productId");
        return Lots.Where(lot => product == null || db.Set<ProductRevision>().Any(x =>
            x.Id == lot.ProductRevisionId && x.ProductId == product));
    }

    private sealed class RunFact
    {
        public ProcessRun Run { get; init; } = null!;
        public StepExecution Step { get; init; } = null!;
        public Lot Lot { get; init; } = null!;
    }

    private IQueryable<RunFact> HistoryRuns(IReadOnlyDictionary<string, string> q)
    {
        var operation = FilterId(q, "operationId");
        return from run in db.Set<ProcessRun>()
               join step in db.Set<StepExecution>() on run.StepExecutionId equals step.Id
               join route in db.Set<RouteStep>() on step.RouteStepId equals route.Id
               join lot in HistoryLots(q) on step.LotId equals lot.Id
               join tool in FilterEquipment(q) on run.EquipmentId equals tool.Id
               where operation == null || route.OperationId == operation
               select new RunFact { Run = run, Step = step, Lot = lot };
    }

    private sealed class CompletionFact
    {
        public WaferCompletion Completion { get; init; } = null!;
        public Wafer Wafer { get; init; } = null!;
        public RunFact Run { get; init; } = null!;
    }

    private IQueryable<CompletionFact> Completions(
        IReadOnlyDictionary<string, string> q, DateTime lower, DateTime to) =>
        from fact in db.Set<WaferCompletion>()
        join run in HistoryRuns(q) on fact.ProcessRunId equals run.Run.Id
        join wafer in db.Set<Wafer>() on fact.WaferId equals wafer.Id
        where fact.CompletedAt >= lower && fact.CompletedAt < to && wafer.FabId == fabId
            && fact.LotIdAtCompletion == run.Lot.Id && fact.WorkOrderId == run.Lot.WorkOrderId
            && wafer.WorkOrderId == fact.WorkOrderId
        select new CompletionFact { Completion = fact, Wafer = wafer, Run = run };

    private IQueryable<WorkbenchRecord> CompletedRecords(
        IReadOnlyDictionary<string, string> q, DateTime lower, DateTime to) =>
        Completions(q, lower, to).Select(x => new WorkbenchRecord
        {
            Id = x.Completion.Id, Kind = "completed", Label = x.Wafer.WaferCode, Status = "Completed",
            OccurredAt = x.Completion.CompletedAt, LotId = x.Completion.LotIdAtCompletion,
            EquipmentId = x.Run.Run.EquipmentId, RunId = x.Run.Run.Id, Quantity = 1, Detail = x.Run.Lot.LotNumber
        });

    private IQueryable<WorkbenchRecord> CycleRecords(
        IReadOnlyDictionary<string, string> q, DateTime lower, DateTime to) =>
        from fact in Completions(q, lower, to)
        join release in db.Set<WaferRelease>() on fact.Wafer.Id equals release.WaferId
        where release.WorkOrderId == fact.Completion.WorkOrderId && release.ReleasedAt <= fact.Completion.CompletedAt
        select new WorkbenchRecord
        {
            Id = fact.Completion.Id, Kind = "completed", Label = fact.Wafer.WaferCode, Status = "Completed",
            OccurredAt = fact.Completion.CompletedAt, LotId = fact.Run.Lot.Id, RunId = fact.Run.Run.Id,
            Quantity = 1, Detail = fact.Run.Lot.LotNumber,
            Minutes = (fact.Completion.CompletedAt - release.ReleasedAt).TotalMinutes
        };

    private IQueryable<RunFact> FirstRuns(IReadOnlyDictionary<string, string> q, DateTime lower, DateTime to) =>
        HistoryRuns(q).Where(x => x.Run.TrackInAt >= lower && x.Run.TrackInAt < to
            && x.Run.TrackInAt >= x.Step.EnteredAt
            && x.Run.Id == db.Set<ProcessRun>().Where(r => r.StepExecutionId == x.Step.Id)
                .OrderBy(r => r.TrackInAt).ThenBy(r => r.Id).Select(r => r.Id).First());

    private IQueryable<WorkbenchRecord> HistoryRecords(
        string kind, IReadOnlyDictionary<string, string> q, DateTime lower, DateTime to)
    {
        if (kind == "completed")
            return CompletedRecords(q, lower, to);
        if (kind == "cycle")
            return CycleRecords(q, lower, to);
        if (kind == "wait")
            return FirstRuns(q, lower, to).Select(x => new WorkbenchRecord
            {
                Id = x.Run.Id, Kind = kind, Label = x.Lot.LotNumber, Status = x.Run.Status,
                OccurredAt = x.Run.TrackInAt, LotId = x.Lot.Id, EquipmentId = x.Run.EquipmentId,
                Quantity = 1, Detail = x.Step.Name, RunId = x.Run.Id,
                Minutes = (x.Run.TrackInAt - x.Step.EnteredAt).TotalMinutes
            });
        if (kind is "passed" or "failed")
        {
            var result = kind == "passed" ? "Passed" : "Failed";
            return from run in HistoryRuns(q)
                   join member in db.Set<ProcessRunWafer>() on run.Run.Id equals member.ProcessRunId
                   join wafer in db.Set<Wafer>() on member.WaferId equals wafer.Id
                   where member.Result == result && member.ProcessedAt >= lower && member.ProcessedAt < to
                       && wafer.FabId == fabId && wafer.WorkOrderId == run.Lot.WorkOrderId
                   select new WorkbenchRecord
                   {
                       Id = member.Id, Kind = kind, Label = wafer.WaferCode, Status = result,
                       OccurredAt = member.ProcessedAt!.Value, LotId = run.Lot.Id, EquipmentId = run.Run.EquipmentId,
                       Quantity = 1, Detail = run.Step.Name, RunId = run.Run.Id
                   };
        }

        var operation = FilterId(q, "operationId");
        var equipmentFilter = EquipmentFilter(q);
        var tools = FilterEquipment(q);
        if (kind == "scrap")
            return from fact in db.Set<WaferDisposition>()
                   join lot in HistoryLots(q) on fact.LotIdAtEvent equals lot.Id
                   join wafer in db.Set<Wafer>() on fact.WaferId equals wafer.Id
                   where fact.DispositionType == "Scrap" && fact.OccurredAt >= lower && fact.OccurredAt < to
                       && wafer.FabId == fabId && wafer.WorkOrderId == lot.WorkOrderId
                       && (!equipmentFilter && operation == null
                           || HistoryRuns(q).Any(run => run.Run.Id == fact.ProcessRunId && run.Lot.Id == lot.Id)
                           || !equipmentFilter && fact.ProcessRunId == null && (
                               from scrap in db.Set<ScrapEntry>()
                               join step in db.Set<StepExecution>() on scrap.StepExecutionId equals step.Id
                               join route in db.Set<RouteStep>() on step.RouteStepId equals route.Id
                               where scrap.Id == fact.ScrapEntryId && step.LotId == lot.Id
                                   && route.OperationId == operation
                               select scrap.Id).Any())
                   select new WorkbenchRecord
                   {
                       Id = fact.Id, Kind = kind, Label = wafer.WaferCode, Status = "Scrap",
                       OccurredAt = fact.OccurredAt, LotId = lot.Id, Quantity = 1, Detail = lot.LotNumber
                   };

        return from check in db.Set<QualityCheck>()
               join lot in HistoryLots(q) on check.LotId equals lot.Id
               where check.AllPassed == false && check.CheckedAt >= lower && check.CheckedAt < to
                   && (!equipmentFilter && operation == null
                       || HistoryRuns(q).Any(run => run.Run.Id == check.ProcessRunId && run.Lot.Id == lot.Id)
                       || !equipmentFilter && check.ProcessRunId == null && (
                           from template in db.Set<QualityCheckTemplate>()
                           join route in db.Set<RouteStep>() on template.RouteStepId equals route.Id
                           where template.Id == check.QualityCheckTemplateId && route.OperationId == operation
                           select template.Id).Any()
                       || equipmentFilter && check.ProcessRunId == null && (
                           from measurement in db.Set<Measurement>()
                           join tool in tools on measurement.MeasurementEquipmentId equals tool.Id
                           where measurement.QualityCheckId == check.Id && measurement.LotId == lot.Id
                               && (operation == null || (
                                   from template in db.Set<QualityCheckTemplate>()
                                   join route in db.Set<RouteStep>() on template.RouteStepId equals route.Id
                                   where template.Id == check.QualityCheckTemplateId && route.OperationId == operation
                                   select template.Id).Any())
                           select measurement.Id).Any())
               select new WorkbenchRecord
               {
                   Id = check.Id, Kind = kind, Label = lot.LotNumber, Status = "Failed",
                   OccurredAt = check.CheckedAt, LotId = lot.Id, Quantity = 1
               };
    }

    private async Task<Shift?> HistoryShift(IReadOnlyDictionary<string, string> q)
    {
        if (FilterId(q, "shiftId") is not Guid id)
            return null;
        if (!can(WorkbenchAccess.Execution))
            require(WorkbenchAccess.Equipment);
        return (await ShiftDefinitions()).SingleOrDefault(x => x.Id == id) ?? throw Missing();
    }

    private static IReadOnlyDictionary<string, string> ShiftFilters(
        IReadOnlyDictionary<string, string> q, Shift? shift) =>
        shift?.LineId is Guid line ? new Dictionary<string, string>(q) { ["lineId"] = line.ToString() } : q;

    private async Task<WorkbenchHistoryDay[]> History(IReadOnlyDictionary<string, string> q)
    {
        var zone = await Zone();
        var today = Today(zone);
        var days = Days(Date(q, "from", today.AddDays(-6)), Date(q, "to", today), 31);
        var shift = await HistoryShift(q);
        var lower = Utc(days[0], shift?.StartTime ?? TimeOnly.MinValue, zone);
        var overnight = shift != null && shift.EndTime < shift.StartTime;
        var lastDay = days[^1];
        var to = Utc(shift == null || overnight ? lastDay.AddDays(1) : lastDay,
            shift?.EndTime ?? TimeOnly.MinValue, zone);
        var timezone = zone.Id;
        var offset = overnight ? -shift!.StartTime.ToTimeSpan().TotalSeconds : 0d;
        var filter = ShiftFilters(q, shift);
        var weekdays = shift == null ? null : ShiftDays(shift);
        Expression<Func<WorkbenchRecord, DateOnly>> bucket = x => DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.OccurredAt, timezone).AddSeconds(offset));
        IQueryable<WorkbenchRecord> Facts(string kind)
        {
            var facts = HistoryRecords(kind, filter, lower, to);
            if (shift == null)
                return facts;
            var starts = shift.StartTime.ToTimeSpan();
            var ends = shift.EndTime.ToTimeSpan();
            return overnight ? facts.Where(x =>
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.OccurredAt, timezone).TimeOfDay >= starts
                || TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.OccurredAt, timezone).TimeOfDay < ends)
                : facts.Where(x =>
                    TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.OccurredAt, timezone).TimeOfDay >= starts
                    && TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.OccurredAt, timezone).TimeOfDay < ends);
        }

        async Task<Dictionary<DateOnly, int>?> Count(string permission, string kind) => can(permission)
            ? await Facts(kind).GroupBy(bucket).Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Date, x => x.Count)
            : null;

        async Task<Dictionary<DateOnly, double?>?> Average(string permission, string kind) => can(permission)
            ? await Facts(kind).GroupBy(bucket).Select(g => new { Date = g.Key, Minutes = g.Average(x => x.Minutes) })
                .ToDictionaryAsync(x => x.Date, x => x.Minutes)
            : null;

        var completed = await Count(WorkbenchAccess.Lots, "completed");
        var passed = await Count(WorkbenchAccess.Execution, "passed");
        var failed = await Count(WorkbenchAccess.Execution, "failed");
        var scrap = await Count(WorkbenchAccess.Quality, "scrap");
        var checks = await Count(WorkbenchAccess.Quality, "failed-checks");
        var waits = await Average(WorkbenchAccess.Execution, "wait");
        var cycles = await Average(WorkbenchAccess.Lots, "cycle");

        return days.Where(day => weekdays == null || weekdays.Contains(Weekday(day)))
            .Select(day => new WorkbenchHistoryDay(day, passed?.GetValueOrDefault(day),
                completed?.GetValueOrDefault(day), failed?.GetValueOrDefault(day), scrap?.GetValueOrDefault(day),
                checks?.GetValueOrDefault(day), waits?.GetValueOrDefault(day), cycles?.GetValueOrDefault(day)))
            .ToArray();
    }
}
