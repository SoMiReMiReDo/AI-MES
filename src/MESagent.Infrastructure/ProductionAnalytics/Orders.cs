using System.Globalization;
using System.Text;
using System.Text.Json;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.LotWaferCarrierGenealogy;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ProductionAnalytics;

internal sealed partial class AnalyticsBackend
{
    private const int ExportLimit = 10000;
    private const string OrderMetricVersion = "work-order-history-v1";
    private static readonly JsonSerializerOptions AuditJson = new(JsonSerializerDefaults.Web);
    private IQueryable<WorkOrder> FabOrders => db.Set<WorkOrder>().AsNoTracking().Where(x => x.FabId == fabId);

    private sealed record OrderFilter(AnalyticsWindow Window, string? Search, Guid? Product, Guid? Revision,
        string? Event, int Page, int Size)
    {
        public Dictionary<string, string> Values => Filters(Window,
            ("q", Search), ("productId", Product), ("revisionId", Revision), ("event", Event));
    }

    private sealed record OrderFacts(
        WorkOrder Order, WorkbenchOption Product, WorkbenchOption Revision, DateTime? TerminalAt,
        bool Verified, int Released, DateTime? FirstRelease, int Completed, DateTime? LastCompletion, int? Scrapped);

    private async Task<OrderFilter> OrderFilters(IReadOnlyDictionary<string, string> query, bool export = false)
    {
        var allowed = new[] { "from", "to", "q", "productId", "revisionId", "event" };
        Validate(query, export ? allowed : [.. allowed, "page", "pageSize"]);
        var search = Value(query, "q");
        if (search?.Length > 100)
            throw Invalid("工单号搜索最多 100 个字符");
        var kind = Value(query, "event");
        if (kind is not (null or "reached" or "closed" or "cancelled" or "missing-time"))
            throw Invalid("无效工单履历事件");
        var (page, size) = Paging(query);
        return new(await DateWindow(query, 30, 90), search,
            Filter(query, "productId"), Filter(query, "revisionId"), kind, page, size);
    }

    private IQueryable<AuditLog> TerminalLogs() => db.Set<AuditLog>().FromSqlInterpolated($"""
        SELECT l.*, l.xmin
        FROM audit.audit_logs l
        WHERE l.entity_type = 'WorkOrder' AND l.action IN ('close', 'cancel')
            AND (l.fab_id = {fabId} AND l.module = 'planning'
                OR l.fab_id IS NULL AND l.after_state->>'fabId' = {fabId.ToString()})
        """).AsNoTracking().Where(x => x.OccurredAt <= now);

    private IQueryable<OrderFacts> OrderRows(IQueryable<WorkOrder> orders)
    {
        var terminal = TerminalLogs();
        var verified = ProductionProgress.Verified(db, FabOrders);
        var showScrap = can(WorkbenchAccess.Quality);
        var releases = db.Set<WaferRelease>().Where(x => x.ReleasedAt <= now);
        var completions = db.Set<WaferCompletion>().Where(x => x.CompletedAt <= now);
        return from order in orders
               join product in db.Set<Product>() on order.ProductId equals product.Id
               join revision in db.Set<ProductRevision>() on order.ProductRevisionId equals revision.Id
               where revision.ProductId == product.Id
               let terminalAt = terminal.Where(x => x.EntityId == order.Id && x.OccurredAt >= order.CreatedAt
                   && (order.Status == "Closed" && x.Action == "close"
                       || order.Status == "Cancelled" && x.Action == "cancel")).Max(x => (DateTime?)x.OccurredAt)
               select new OrderFacts(order, new(product.Id, product.Code, product.Name),
                   new(revision.Id, revision.RevisionCode, revision.RevisionCode), terminalAt,
                   verified.Any(x => x.Id == order.Id), releases.Count(x => x.WorkOrderId == order.Id),
                   releases.Where(x => x.WorkOrderId == order.Id).Min(x => (DateTime?)x.ReleasedAt),
                   completions.Count(x => x.WorkOrderId == order.Id),
                   completions.Where(x => x.WorkOrderId == order.Id).Max(x => (DateTime?)x.CompletedAt), showScrap
                       ? db.Set<Wafer>().Count(x => x.WorkOrderId == order.Id && x.FabId == fabId
                           && x.LifecycleStatus == "Scrapped"
                           && releases.Any(r => r.WorkOrderId == order.Id && r.WaferId == x.Id))
                       : null);
    }

    private async Task<AnalyticsWorkOrder[]> MatchingOrders(OrderFilter filter)
    {
        var lower = filter.Window.Lower;
        var upper = filter.Window.Upper;
        var search = filter.Search?.ToUpperInvariant();
        var candidates = FabOrders.Where(x =>
            (filter.Product == null || x.ProductId == filter.Product)
            && (filter.Revision == null || x.ProductRevisionId == filter.Revision)
            && (search == null || x.OrderNo.ToUpper().Contains(search))
            && (x.Status == "Released" || x.Status == "Closed" || x.Status == "Cancelled"));
        // Prefilter by source events before loading production facts, including orders whose caches predate F5.
        if (filter.Search == null)
        {
            var terminal = TerminalLogs();
            candidates = candidates.Where(order =>
                terminal.Any(log => log.EntityId == order.Id && log.OccurredAt >= lower && log.OccurredAt < upper)
                || db.Set<WaferCompletion>().Any(c => c.WorkOrderId == order.Id
                    && c.CompletedAt >= lower && c.CompletedAt < upper));
        }

        var rows = await OrderRows(candidates).ToArrayAsync();
        var reached = await ReachedDates(rows);
        return rows.Select(row => OrderView(
                row, reached.TryGetValue(row.Order.Id, out var date) ? date : null, filter.Window.Zone))
            .Where(x => (filter.Event == null || x.Event == filter.Event)
                && (x.EventAt >= lower && x.EventAt < upper || x.Event == "missing-time" && filter.Search != null))
            .OrderByDescending(x => x.EventAt).ThenBy(x => x.OrderNo, StringComparer.Ordinal).ThenBy(x => x.Id)
            .ToArray();
    }

    private async Task<Dictionary<Guid, DateTime>> ReachedDates(OrderFacts[] rows)
    {
        var reached = rows.Where(x => x.Verified && x.Order.PlannedQty > 0 && x.Completed == x.Order.PlannedQty
            && x.LastCompletion != null).ToDictionary(x => x.Order.Id, x => x.LastCompletion!.Value);
        // A revised legacy plan can be smaller than the final quantity. Use the threshold crossing instant.
        var exceeded = rows.Where(x => x.Verified && x.Order.PlannedQty > 0 && x.Completed > x.Order.PlannedQty)
            .ToDictionary(x => x.Order.Id, x => x.Order.PlannedQty);
        if (exceeded.Count == 0)
            return reached;
        var ids = exceeded.Keys.ToArray();
        var dates = await db.Set<WaferCompletion>().Where(x => ids.Contains(x.WorkOrderId) && x.CompletedAt <= now)
            .OrderBy(x => x.CompletedAt).ThenBy(x => x.Id).Select(x => new { x.WorkOrderId, x.CompletedAt })
            .ToArrayAsync();
        foreach (var group in dates.GroupBy(x => x.WorkOrderId))
            reached[group.Key] = group.ElementAt(exceeded[group.Key] - 1).CompletedAt;
        return reached;
    }

    private AnalyticsWorkOrder OrderView(OrderFacts row, DateTime? reached, TimeZoneInfo zone)
    {
        var order = row.Order;
        var terminal = order.Status is "Closed" or "Cancelled";
        var eventAt = terminal ? row.TerminalAt : reached;
        var kind = eventAt == null ? terminal ? "missing-time" : "open"
            : row.TerminalAt != null ? order.Status.ToLowerInvariant() : "reached";
        bool? onTime = reached is { } done && order.DueDate is { } due
            ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(done, zone)) <= due : null;
        double? productionMinutes = reached is { } completed && row.FirstRelease is { } released
            && completed >= released ? (completed - released).TotalMinutes : null;
        var missing = new List<string>();
        if (!row.Verified)
            missing.Add("incomplete-production-history");
        if (terminal && row.TerminalAt == null)
            missing.Add("terminal-time-missing");
        if (!can(WorkbenchAccess.Quality))
            missing.Add("quality-restricted");
        if (reached != null && productionMinutes == null)
            missing.Add("release-time-missing");
        var evidence = new List<AnalyticsEvidence>
        {
            new("work-order-history", order.Id, eventAt, $"/analytics/work-orders/{order.Id}"),
            Trace("work-order", order.Id, eventAt)
        };
        return new(order.Id, order.OrderNo, row.Product, row.Revision, order.Status, kind, eventAt,
            reached, row.TerminalAt, order.DueDate, order.PlannedQty, row.Verified ? row.Released : null,
            row.Verified ? row.Completed : null, row.Verified ? row.Scrapped : null,
            onTime, productionMinutes, missing.ToArray(), evidence.ToArray());
    }

    private async Task<AnalyticsOrders> Orders(IReadOnlyDictionary<string, string> query)
    {
        var filter = await OrderFilters(query);
        var rows = await MatchingOrders(filter);
        var dated = rows.Where(x => x.EventAt != null).ToArray();
        var reached = dated.Count(x => x.CompletedAt != null);
        var closedBelow = dated.Count(x => x.Status == "Closed" && x.Completed < x.Planned);
        var onTime = dated.Count(x => x.OnTime == true);
        var withDueDate = dated.Count(x => x.OnTime != null);
        var durations = dated.Select(x => x.ProductionMinutes).OfType<double>().ToArray();
        long? Sum(Func<AnalyticsWorkOrder, int?> pick) =>
            dated.Any(x => pick(x) == null) ? null : dated.Sum(x => (long)(pick(x) ?? 0));
        var summary = new AnalyticsOrderSummary(dated.Length, dated.Sum(x => (long)x.Planned),
            Sum(x => x.Released), Sum(x => x.Completed), can(WorkbenchAccess.Quality) ? Sum(x => x.Scrapped) : null,
            reached, closedBelow, onTime, withDueDate, Ratio(onTime, withDueDate),
            durations.Length > 0 ? durations.Average() : null, durations.Length);
        var items = rows.Skip((filter.Page - 1) * filter.Size).Take(filter.Size).ToArray();
        var opts = await (from revision in db.Set<ProductRevision>()
                          join product in db.Set<Product>() on revision.ProductId equals product.Id
                          where FabOrders.Any(o => o.ProductRevisionId == revision.Id && o.ProductId == product.Id)
                          orderby product.Code, revision.RevisionCode, revision.Id
                          select new
                          {
                              Product = new WorkbenchOption(product.Id, product.Code, product.Name),
                              Revision = new WorkbenchOption(revision.Id, product.Code, revision.RevisionCode),
                              revision.ProductId
                          }).ToArrayAsync();
        return new(Context(now, filter.Window.Zone.Id, OrderMetricVersion, filter.Values, WaferUnits,
                rows.SelectMany(x => x.Missing).Concat(can(WorkbenchAccess.Quality) ? [] : ["quality-restricted"]),
                items.SelectMany(x => x.Evidence)),
            summary, new(items, rows.Length, filter.Page, filter.Size),
            opts.Select(x => x.Product).Distinct().ToArray(),
            opts.Where(x => filter.Product == null || x.ProductId == filter.Product).Select(x => x.Revision).ToArray());
    }

    private async Task<AnalyticsOrderDetail> OrderDetail(Guid id, IReadOnlyDictionary<string, string> query)
    {
        Validate(query, "page", "pageSize");
        var (page, size) = Paging(query);
        var row = await OrderRows(FabOrders.Where(x => x.Id == id
            && (x.Status == "Released" || x.Status == "Closed" || x.Status == "Cancelled"))).SingleOrDefaultAsync()
            ?? throw Missing();
        var zone = await Zone();
        var reached = await ReachedDates([row]);
        var order = OrderView(row, reached.TryGetValue(id, out var reachedAt) ? reachedAt : null, zone);
        if (order.Event == "open")
            throw Missing();
        var snapshot = await db.Set<WorkOrderSnapshot>().AsNoTracking().Where(x => x.WorkOrderId == id
            && x.EffectiveFrom <= now).OrderBy(x => x.Version).FirstOrDefaultAsync();
        AnalyticsOrderBaseline? baseline = null;
        var missing = order.Missing.ToList();
        if (snapshot != null)
        {
            using var doc = JsonDocument.Parse(snapshot.Snapshot);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("plan", out var plan) && plan.ValueKind == JsonValueKind.Object)
            {
                baseline = new(snapshot.Version, snapshot.EffectiveFrom, PlanInt(plan, "plannedQty"),
                    PlanInt(plan, "priority"),
                    plan.TryGetProperty("dueDate", out var date) && date.ValueKind == JsonValueKind.String
                        && DateOnly.TryParseExact(date.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var due) ? due : null);
            }
        }
        if (baseline?.Planned == null && row.Order.SnapshotVersion > 0)
            missing.Add("release-baseline-missing");

        PageView<AnalyticsOrderLot>? lots = null;
        var evidence = order.Evidence.ToList();
        if (can(WorkbenchAccess.Lots))
        {
            var source = db.Set<Lot>().Where(x => x.FabId == fabId && x.WorkOrderId == id);
            var records = await source.OrderBy(x => x.LotNumber).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size)
                .Select(x => new { x.Id, x.LotNumber, x.Status, x.LotAssignedAt }).ToArrayAsync();
            var items = records.Select(x => new AnalyticsOrderLot(x.Id, x.LotNumber, x.Status,
                Trace("lot", x.Id, x.LotAssignedAt))).ToArray();
            lots = new(items, await source.CountAsync(), page, size);
            evidence.AddRange(items.Select(x => x.Evidence));
        }
        else
            missing.Add("lots-restricted");

        if (can(WorkbenchAccess.Execution))
        {
            var runs = await (from run in db.Set<ProcessRun>()
                              join step in db.Set<StepExecution>() on run.StepExecutionId equals step.Id
                              join lot in db.Set<Lot>() on step.LotId equals lot.Id
                              where lot.WorkOrderId == id && lot.FabId == fabId && run.TrackInAt <= now
                              orderby run.TrackInAt descending, run.Id
                              select new { run.Id, run.TrackInAt }).Take(5).ToArrayAsync();
            evidence.InsertRange(order.Evidence.Length, runs.Select(x => new AnalyticsEvidence(
                "process-run", x.Id, x.TrackInAt, $"/execution?run={x.Id}")));
        }

        if (can(WorkbenchAccess.Quality))
        {
            var issues = await (from issue in db.Set<Issue>()
                                join lot in db.Set<Lot>() on issue.LotId equals lot.Id
                                where issue.WorkOrderId == id && lot.WorkOrderId == id && lot.FabId == fabId
                                orderby issue.ReportedAt descending, issue.Id
                                select new { issue.Id, issue.ReportedAt }).Take(5).ToArrayAsync();
            evidence.InsertRange(order.Evidence.Length, issues.Select(x => new AnalyticsEvidence(
                "quality-issue", x.Id, x.ReportedAt,
                $"/quality?view=issues&record={x.Id}")));
        }
        return new(Context(now, zone.Id, OrderMetricVersion, new Dictionary<string, string> { ["id"] = id.ToString() },
            WaferUnits, missing, evidence), order, baseline, lots);
    }

    private static int? PlanInt(JsonElement plan, string key) =>
        plan.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var number) ? number : null;

    private async Task<AnalyticsExport> Export(IReadOnlyDictionary<string, string> query)
    {
        var filter = await OrderFilters(query, export: true);
        var rows = await MatchingOrders(filter);
        if (rows.Length > ExportLimit)
            throw Invalid($"导出最多 {ExportLimit} 行，请缩小筛选范围");
        var quality = can(WorkbenchAccess.Quality);
        var header = new List<string>
        {
            "工单号", "产品编码", "产品修订", "状态", "履历事件",
            "事件时间 UTC", "达到计划时间 UTC", "终态时间 UTC", "厂区交期",
            "计划片数", "已投放片数", "终点完工片数", "按期达成", "生产分钟", "缺数原因"
        };
        if (quality)
            header.Add("报废片数");
        var csv = new StringBuilder().AppendLine(string.Join(',', header.Select(Csv)));
        foreach (var row in rows)
        {
            var cells = new List<string?>
            {
                row.OrderNo, row.Product.Code, row.Revision.Code, row.Status, row.Event,
                row.EventAt?.ToString("O"), row.CompletedAt?.ToString("O"), row.TerminalAt?.ToString("O"),
                row.DueDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                row.Planned.ToString(CultureInfo.InvariantCulture),
                row.Released?.ToString(CultureInfo.InvariantCulture),
                row.Completed?.ToString(CultureInfo.InvariantCulture), row.OnTime?.ToString(),
                row.ProductionMinutes?.ToString("G", CultureInfo.InvariantCulture), string.Join(", ", row.Missing)
            };
            if (quality)
                cells.Add(row.Scrapped?.ToString(CultureInfo.InvariantCulture));
            csv.AppendLine(string.Join(',', cells.Select(Csv)));
        }

        db.Add(new AuditLog
        {
            UserId = actor.UserId, FabId = fabId, Module = "analytics", EntityId = fabId,
            EntityType = "WorkOrderHistory", Action = "Exported", OccurredAt = now, CorrelationId = actor.CorrelationId,
            SourceChannel = "Web", AfterState = JsonSerializer.Serialize(
                new { filters = filter.Values, rows = rows.Length, metricVersion = OrderMetricVersion }, AuditJson)
        });
        return new(Encoding.UTF8.GetBytes("\uFEFF" + csv), $"work-orders-{filter.Window.To:yyyy-MM-dd}.csv");
    }

    private static string Csv(string? value)
    {
        value ??= "";
        var trimmed = value.TrimStart();
        if (trimmed.Length != 0 && trimmed[0] is '=' or '+' or '-' or '@' or '＝' or '＋' or '－' or '＠'
            || value.Length != 0 && value[0] is '\t' or '\r' or '\n')
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
