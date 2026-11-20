using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.EquipmentMaintenanceResources;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ProductionAnalytics;

internal sealed partial class AnalyticsBackend
{
    private const string ScrapMetricVersion = "wafer-scrap-v1";

    private sealed record ScrapFilter(
        AnalyticsWindow Window, Guid? Product, Guid? Operation, Guid? Reason, Guid? Equipment, int Page, int Size)
    {
        public Dictionary<string, string> Values => Filters(Window,
            ("productId", Product), ("operationId", Operation), ("reasonId", Reason), ("equipmentId", Equipment));
    }

    private sealed class ScrapFact
    {
        public required Guid Id { get; init; }
        public required DateTime At { get; init; }
        public required Guid EntryId { get; init; }
        public required Guid WaferId { get; init; }
        public required string WaferCode { get; init; }
        public required Guid LotId { get; init; }
        public required string LotNumber { get; init; }
        public required Guid ProductId { get; init; }
        public required string ProductCode { get; init; }
        public required string ProductName { get; init; }
        public required Guid ReasonId { get; init; }
        public required string ReasonCode { get; init; }
        public required string ReasonName { get; init; }
        public Guid? OperationId { get; init; }
        public string? OperationCode { get; init; }
        public string? OperationName { get; init; }
        public Guid? EquipmentId { get; init; }
        public string? EquipmentCode { get; init; }
        public string? EquipmentName { get; init; }
    }

    private async Task<ScrapFilter> ScrapFilters(IReadOnlyDictionary<string, string> query)
    {
        Validate(query, "from", "to", "productId", "operationId", "reasonId", "equipmentId", "page", "pageSize");
        if (Value(query, "equipmentId") != null)
            require(WorkbenchAccess.Equipment);
        var (page, size) = Paging(query);
        return new(await DateWindow(query, 30, 90), Filter(query, "productId"), Filter(query, "operationId"),
            Filter(query, "reasonId"), Filter(query, "equipmentId"), page, size);
    }

    private IQueryable<ScrapFact> ScrapFacts(DateTime lower, DateTime upper)
    {
        var scopes = EquipmentResources.EquipmentScope(db, fabId).AsNoTracking();
        var showEquipment = can(WorkbenchAccess.Equipment);
        return from fact in db.Set<WaferDisposition>()
               join entry in db.Set<ScrapEntry>() on fact.ScrapEntryId equals entry.Id
               join lot in db.Set<Lot>() on fact.LotIdAtEvent equals lot.Id
               join wafer in db.Set<Wafer>() on fact.WaferId equals wafer.Id
               join order in FabOrders on lot.WorkOrderId equals order.Id
               join revision in db.Set<ProductRevision>() on lot.ProductRevisionId equals revision.Id
               join product in db.Set<Product>() on revision.ProductId equals product.Id
               join reason in db.Set<ReasonCode>() on fact.ReasonCodeId equals reason.Id
               where fact.DispositionType == "Scrap" && fact.OccurredAt >= lower && fact.OccurredAt < upper
                   && lot.FabId == fabId && wafer.FabId == fabId && wafer.WorkOrderId == order.Id
                   && entry.WorkOrderId == order.Id && entry.ReasonCodeId == fact.ReasonCodeId
                   && db.Set<LotMembership>().Any(x => x.WaferId == wafer.Id && x.LotId == lot.Id
                       && x.ValidFrom <= fact.OccurredAt && x.ValidTo >= fact.OccurredAt)
                   && db.Set<WaferRelease>().Any(x => x.WaferId == wafer.Id && x.WorkOrderId == order.Id
                       && x.ReleasedAt <= fact.OccurredAt)
               from run in db.Set<ProcessRun>().Where(x => x.Id == fact.ProcessRunId
                   && x.TrackOutAt <= fact.OccurredAt && x.TrackInAt <= x.TrackOutAt
                   && db.Set<StepExecution>().Any(actual => actual.Id == x.StepExecutionId && actual.LotId == lot.Id
                       && actual.RouteVersionId == lot.RouteVersionId && actual.EnteredAt <= x.TrackInAt
                       && (entry.StepExecutionId == null || actual.Id == entry.StepExecutionId))).DefaultIfEmpty()
               from step in db.Set<StepExecution>().Where(x => x.Id == ((Guid?)run.StepExecutionId ?? entry.StepExecutionId)
                   && x.LotId == lot.Id && x.RouteVersionId == lot.RouteVersionId && x.EnteredAt <= fact.OccurredAt)
                   .DefaultIfEmpty()
               from route in db.Set<RouteStep>().Where(x => x.Id == step.RouteStepId
                   && x.RouteVersionId == lot.RouteVersionId).DefaultIfEmpty()
               from operation in db.Set<Operation>().Where(x => x.Id == route.OperationId).DefaultIfEmpty()
               from tool in scopes.Where(x => showEquipment && x.Id == run.EquipmentId).DefaultIfEmpty()
               select new ScrapFact
               {
                   Id = fact.Id, At = fact.OccurredAt, EntryId = entry.Id,
                   WaferId = wafer.Id, WaferCode = wafer.WaferCode,
                   LotId = lot.Id, LotNumber = lot.LotNumber, ProductId = product.Id, ProductCode = product.Code,
                   ProductName = product.Name, ReasonId = reason.Id, ReasonCode = reason.Code, ReasonName = reason.Name,
                   OperationId = (Guid?)operation.Id, OperationCode = operation.Code, OperationName = operation.Name,
                   EquipmentId = (Guid?)tool.Id, EquipmentCode = tool.Code, EquipmentName = tool.Name
               };
    }

    private static IQueryable<ScrapFact> SelectScrap(IQueryable<ScrapFact> rows, ScrapFilter filter) =>
        rows.Where(x => (filter.Product == null || x.ProductId == filter.Product)
            && (filter.Operation == null || x.OperationId == filter.Operation)
            && (filter.Equipment == null || x.EquipmentId == filter.Equipment)
            && (filter.Reason == null || x.ReasonId == filter.Reason));

    private async Task<AnalyticsScrap> Scrap(IReadOnlyDictionary<string, string> query)
    {
        var filter = await ScrapFilters(query);
        var lower = filter.Window.Lower;
        var upper = Min(now, filter.Window.Upper);
        var all = ScrapFacts(TimeFloor, now);
        var rows = SelectScrap(ScrapFacts(lower, upper), filter);
        var count = await rows.CountAsync();
        var totalEntries = await rows.Select(x => x.EntryId).Distinct().CountAsync();
        var reasons = await rows.GroupBy(x => new { x.ReasonId, x.ReasonCode, x.ReasonName })
            .Select(g => new AnalyticsBreakdown(g.Key.ReasonId, g.Key.ReasonCode, g.Key.ReasonName, g.Count(), null))
            .ToArrayAsync();
        var cumulative = 0;
        reasons = reasons.OrderByDescending(x => x.Wafers).ThenBy(x => x.Code, StringComparer.Ordinal).ThenBy(x => x.Id)
            .Select(x => x with { CumulativeRatio = Ratio(cumulative += x.Wafers, count) }).ToArray();
        var zone = filter.Window.Zone.Id;
        var daily = await rows.GroupBy(x => DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(x.At, zone)))
            .Select(g => new { Date = g.Key, Wafers = g.Count() }).ToDictionaryAsync(x => x.Date, x => x.Wafers);
        var days = filter.Window.Days.Select(day => new AnalyticsScrapDay(day, daily.GetValueOrDefault(day))).ToArray();
        var operations = await rows.GroupBy(x => new { x.OperationId, x.OperationCode, x.OperationName })
            .Select(g => new AnalyticsBreakdown(g.Key.OperationId, g.Key.OperationCode,
                g.Key.OperationName ?? "归属未知", g.Count(), null)).ToArrayAsync();
        var equipment = can(WorkbenchAccess.Equipment) ? await rows
            .GroupBy(x => new { x.EquipmentId, x.EquipmentCode, x.EquipmentName })
            .Select(g => new AnalyticsBreakdown(g.Key.EquipmentId, g.Key.EquipmentCode,
                g.Key.EquipmentName ?? "归属未知", g.Count(), null)).ToArrayAsync() : null;
        var missing = new List<string>();
        if (equipment == null)
            missing.Add("equipment-restricted");
        var (completed, settled, ratio) = await Settlement(filter, lower, upper, count, missing);
        var samples = await rows.OrderByDescending(x => x.At).ThenBy(x => x.Id).Take(6).ToArrayAsync();
        return new(Context(now, zone, ScrapMetricVersion, filter.Values, WaferUnits,
                missing, samples.SelectMany(Evidence)),
            count, totalEntries, completed, settled, ratio, reasons, days, Breakdown(operations),
            equipment == null ? null : Breakdown(equipment),
            await all.GroupBy(x => new { Id = x.ProductId, Code = x.ProductCode, Name = x.ProductName })
                .OrderBy(g => g.Key.Code).ThenBy(g => g.Key.Id)
                .Select(g => new WorkbenchOption(g.Key.Id, g.Key.Code, g.Key.Name)).ToArrayAsync(),
            await all.Where(x => x.OperationId != null)
                .GroupBy(x => new { Id = x.OperationId!.Value, Code = x.OperationCode!, Name = x.OperationName! })
                .OrderBy(g => g.Key.Code).ThenBy(g => g.Key.Id)
                .Select(g => new WorkbenchOption(g.Key.Id, g.Key.Code, g.Key.Name)).ToArrayAsync(),
            await all.GroupBy(x => new { Id = x.ReasonId, Code = x.ReasonCode, Name = x.ReasonName })
                .OrderBy(g => g.Key.Code).ThenBy(g => g.Key.Id)
                .Select(g => new WorkbenchOption(g.Key.Id, g.Key.Code, g.Key.Name)).ToArrayAsync(),
            equipment == null ? null : await all.Where(x => x.EquipmentId != null)
                .GroupBy(x => new { Id = x.EquipmentId!.Value, Code = x.EquipmentCode!, Name = x.EquipmentName! })
                .OrderBy(g => g.Key.Code).ThenBy(g => g.Key.Id)
                .Select(g => new WorkbenchOption(g.Key.Id, g.Key.Code, g.Key.Name)).ToArrayAsync());
    }

    private static readonly DateTime TimeFloor = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    private async Task<(int? Completed, int? Settled, double? Ratio)> Settlement(
        ScrapFilter filter, DateTime lower, DateTime upper, int scraps, List<string> missing)
    {
        if (!can(WorkbenchAccess.Lots))
        {
            missing.Add("lots-restricted");
            return (null, null, null);
        }
        if (filter.Operation != null || filter.Equipment != null || filter.Reason != null)
        {
            missing.Add("settlement-scope");
            return (null, null, null);
        }

        var lots = from lot in db.Set<Lot>()
                   join revision in db.Set<ProductRevision>() on lot.ProductRevisionId equals revision.Id
                   where lot.FabId == fabId && (filter.Product == null || revision.ProductId == filter.Product)
                   select lot;
        var completions = from completion in db.Set<WaferCompletion>()
                          join lot in lots on completion.LotIdAtCompletion equals lot.Id
                          join wafer in db.Set<Wafer>() on completion.WaferId equals wafer.Id
                          where completion.CompletedAt >= lower && completion.CompletedAt < upper
                              && lot.WorkOrderId == completion.WorkOrderId && wafer.FabId == fabId
                              && wafer.WorkOrderId == completion.WorkOrderId
                          select completion;
        var dispositions = from fact in db.Set<WaferDisposition>()
                           join lot in lots on fact.LotIdAtEvent equals lot.Id
                           where fact.DispositionType == "Scrap" && fact.OccurredAt >= lower && fact.OccurredAt < upper
                           select fact.Id;
        if (await lots.AnyAsync(x => x.Status == "Completed" && x.CompletedAt >= lower && x.CompletedAt < upper
                && (x.SnapshotVersion == null
                    || x.ProducedQty > db.Set<WaferCompletion>().Count(c => c.LotIdAtCompletion == x.Id)))
            || await dispositions.CountAsync() != scraps)
        {
            missing.Add("incomplete-settlement-history");
            return (null, null, null);
        }

        var completed = await completions.CountAsync();
        var settled = completed + scraps;
        if (settled == 0)
            missing.Add("no-settled-wafers");
        return (completed, settled, Ratio(scraps, settled));
    }

    private static AnalyticsBreakdown[] Breakdown(AnalyticsBreakdown[] values) =>
        values.OrderByDescending(x => x.Wafers).ThenBy(x => x.Code, StringComparer.Ordinal).ThenBy(x => x.Id).ToArray();

    private AnalyticsEvidence[] Evidence(ScrapFact row)
    {
        var links = new List<AnalyticsEvidence> { QualityScrap(row) };
        if (can("production.wafers.read"))
            links.Add(Trace("wafer", row.WaferId, row.At));
        if (can(WorkbenchAccess.Lots))
            links.Add(Trace("lot", row.LotId, row.At));
        return links.ToArray();
    }

    private static AnalyticsEvidence QualityScrap(ScrapFact row) =>
        new("quality-scrap", row.EntryId, row.At, $"/quality?view=scraps&record={row.EntryId}");

    private async Task<AnalyticsScrapRecords> ScrapRecords(IReadOnlyDictionary<string, string> query)
    {
        var filter = await ScrapFilters(query);
        var rows = SelectScrap(ScrapFacts(filter.Window.Lower, Min(now, filter.Window.Upper)), filter);
        var total = await rows.CountAsync();
        var selected = await rows.OrderByDescending(x => x.At).ThenBy(x => x.Id)
            .Skip((filter.Page - 1) * filter.Size).Take(filter.Size).ToArrayAsync();
        var items = selected.Select(x => new AnalyticsScrapRecord(x.Id, x.At, x.EntryId, x.LotNumber, x.WaferCode,
            new(x.ProductId, x.ProductCode, x.ProductName), new(x.ReasonId, x.ReasonCode, x.ReasonName),
            x.OperationId is Guid operation ? new(operation, x.OperationCode!, x.OperationName!) : null,
            x.EquipmentId is Guid tool ? new(tool, x.EquipmentCode!, x.EquipmentName!) : null, Evidence(x))).ToArray();
        var missing = can(WorkbenchAccess.Equipment) ? Array.Empty<string>() : ["equipment-restricted"];
        return new(Context(now, filter.Window.Zone.Id, ScrapMetricVersion, filter.Values, WaferUnits, missing,
            items.SelectMany(x => x.Evidence)), new(items, total, filter.Page, filter.Size));
    }
}
