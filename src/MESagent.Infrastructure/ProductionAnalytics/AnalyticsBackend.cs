using System.Globalization;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.MaterialConsumptionInventory;
using MESagent.Infrastructure.Persistence;
using MESagent.Infrastructure.WorkbenchDashboardsAlertsScheduling;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.ProductionAnalytics;

internal sealed partial class AnalyticsBackend(
    MesDbContext db, Guid fabId, RequestActor actor, Action<string> require, Func<string, bool> can, TimeProvider clock)
{
    private const string Planning = "planning.work-orders.read";
    private const string Materials = "materials.wafer-sources.read";
    private readonly DateTime now = clock.GetUtcNow().UtcDateTime;

    private static readonly IReadOnlyDictionary<string, string> WaferUnits =
        new Dictionary<string, string>
        {
            ["quantity"] = "wafer", ["productionMinutes"] = "minute", ["ratio"] = "fraction"
        };

    internal async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest r)
    {
        var parts = r.Path.Split('/');
        switch (parts[1])
        {
            case "work-orders":
                require(Planning);
                if (r.Method == "POST" && parts is [_, _, "export"])
                    return new(await Export(r.Query));
                if (r.Method == "GET" && parts.Length == 2)
                    return new(await Orders(r.Query));
                if (r.Method == "GET" && parts.Length == 3)
                    return new(await OrderDetail(Guid.Parse(parts[2]), r.Query));
                break;
            case "scrap" when r.Method == "GET":
                require(WorkbenchAccess.Quality);
                if (parts.Length == 2)
                    return new(await Scrap(r.Query));
                if (parts is [_, _, "records"])
                    return new(await ScrapRecords(r.Query));
                break;
            case "oee" when r.Method == "GET":
                require(WorkbenchAccess.Equipment);
                require(WorkbenchAccess.Execution);
                var window = await DateWindow(r.Query, 7, 31);
                var workbench = new WorkbenchBackend(db, fabId, actor, require, can, clock);
                if (parts.Length == 2)
                    return new(await workbench.AnalyticsOee(window, r.Query));
                if (parts is [_, _, "shifts"])
                    return new(await workbench.AnalyticsShiftRows(window, r.Query));
                break;
            case "cost-readiness" when r.Method == "GET" && parts.Length == 2:
                require(Planning);
                return new(await CostReadiness(r.Query));
        }

        throw Missing();
    }

    private async Task<AnalyticsCostReadiness> CostReadiness(IReadOnlyDictionary<string, string> query)
    {
        Validate(query);
        var zone = await Zone();
        var completion = await (from fact in db.Set<WaferCompletion>()
                                join wafer in db.Set<Wafer>() on fact.WaferId equals wafer.Id
                                join order in db.Set<WorkOrder>() on fact.WorkOrderId equals order.Id
                                where order.FabId == fabId && wafer.FabId == fabId && wafer.WorkOrderId == order.Id
                                select fact.Id).AnyAsync();
        var materialAccess = can(Materials);
        var origins = WaferOrigins.Lots(db, fabId);
        var origin = materialAccess
            ? await (from source in origins
                     where db.Set<Wafer>().Any(x => x.FabId == fabId && x.OriginMaterialLotId == source.Lot.Id)
                     orderby source.Lot.ReceivedAt descending, source.Lot.Id
                     select new { source.Lot.Id, source.Lot.ReceivedAt }).FirstOrDefaultAsync()
            : null;
        var sources = new AnalyticsCostSource[]
        {
            new("completion", completion ? "available" : "no-data", "工单终点完工片数",
                completion ? new("work-order-history", fabId, null, "/analytics/work-orders") : null),
            new("wafer-origin", !materialAccess ? "restricted" : origin != null ? "available" : "no-data", "硅片来料追溯",
                origin != null ? new("wafer-origin", origin.Id, origin.ReceivedAt, $"/materials?lot={origin.Id}") : null),
            new("consumption", "pending", "实际耗用与单位换算", null),
            new("pricing", "pending", "财务／ERP 计价快照与币种", null),
            new("labor", "pending", "人工计价与分摊", null),
            new("overhead", "pending", "附加费用归集、月结与更正", null)
        };
        return new(Context(now, zone.Id, "cost-readiness-v1", new Dictionary<string, string>(), WaferUnits,
            sources.Where(x => x.Status != "available").Select(x => x.Kind + ":" + x.Status),
            sources.Select(x => x.Evidence).OfType<AnalyticsEvidence>()), sources, null, null);
    }

    private async Task<TimeZoneInfo> Zone() => TimeZoneInfo.FindSystemTimeZoneById(
        await db.Set<Fab>().Where(x => x.Id == fabId).Select(x => x.Timezone).SingleAsync());

    private async Task<AnalyticsWindow> DateWindow(IReadOnlyDictionary<string, string> query, int size, int max)
    {
        var zone = await Zone();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, zone));
        var to = Date(query, "to", today);
        var from = Date(query, "from", to.AddDays(1 - size));
        var count = to.DayNumber - from.DayNumber + 1;
        if (count < 1 || count > max)
            throw Invalid($"日期范围须为 1～{max} 天");
        return new(zone, from, to, Enumerable.Range(0, count).Select(from.AddDays).ToArray());
    }

    private static DateOnly Date(IReadOnlyDictionary<string, string> query, string key, DateOnly fallback)
    {
        if (Value(query, key) is not { } value)
            return fallback;
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)
            || day.Year is < 1900 or > 2200)
            throw Invalid("日期须为 YYYY-MM-DD");
        return day;
    }

    internal static string? Value(IReadOnlyDictionary<string, string> query, string key) =>
        query.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    internal static Guid? Filter(IReadOnlyDictionary<string, string> query, string key) =>
        Value(query, key) is not { } value ? null : Guid.TryParse(value, out var id) && id != Guid.Empty
            ? id : throw Invalid($"{key} 须为有效 UUID");

    internal static (int Page, int Size) Paging(IReadOnlyDictionary<string, string> query) =>
        (Number(query, "page", 1, 1000000), Number(query, "pageSize", 20, 100));

    private static int Number(IReadOnlyDictionary<string, string> query, string key, int fallback, int max) =>
        Value(query, key) is not { } value ? fallback :
            int.TryParse(value, out var number) && number >= 1 && number <= max
                ? number : throw Invalid($"{key} 超出范围");

    internal static void Validate(IReadOnlyDictionary<string, string> query, params string[] allowed)
    {
        if (query.Keys.Any(key => !allowed.Contains(key, StringComparer.Ordinal)))
            throw Invalid("存在不支持的筛选参数");
    }

    internal static Dictionary<string, string> Filters(
        AnalyticsWindow window, params (string Key, object? Value)[] filters)
    {
        var result = new Dictionary<string, string>
        {
            ["from"] = window.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["to"] = window.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
        foreach (var (key, value) in filters)
        {
            if (value is not null)
                result[key] = Convert.ToString(value, CultureInfo.InvariantCulture)!;
        }
        return result;
    }

    internal static AnalyticsContext Context(
        DateTime asOf, string zone, string version, IReadOnlyDictionary<string, string> filters,
        IReadOnlyDictionary<string, string> units, IEnumerable<string> missing,
        IEnumerable<AnalyticsEvidence> evidence) =>
        new(asOf, zone, version, filters, units, missing.Distinct().Order(StringComparer.Ordinal).ToArray(),
            evidence.Distinct().Take(20).ToArray());

    internal static double? Ratio(double numerator, double denominator) =>
        denominator > 0 ? numerator / denominator : null;

    private static AnalyticsEvidence Trace(string kind, Guid id, DateTime? at = null) =>
        new(kind, id, at, $"/traceability?kind={kind}&id={id}");

    internal static AppException Invalid(string message) => new(400, "InvalidAnalyticsInput", message);

    private static AppException Missing() => new(404, "ResourceNotFound", "记录不存在或不可访问");
}

internal sealed record AnalyticsWindow(TimeZoneInfo Zone, DateOnly From, DateOnly To, DateOnly[] Days)
{
    internal DateTime Lower => WorkbenchBackend.Utc(From, TimeOnly.MinValue, Zone);

    internal DateTime Upper => WorkbenchBackend.Utc(To.AddDays(1), TimeOnly.MinValue, Zone);
}
