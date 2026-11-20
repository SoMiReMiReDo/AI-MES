using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.EquipmentMaintenanceResources;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.WorkbenchDashboardsAlertsScheduling;

internal sealed partial class WorkbenchBackend(
    MesDbContext db, Guid fabId, RequestActor actor, Action<string> require, Func<string, bool> can, TimeProvider clock)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private DateTime Now { get; } = clock.GetUtcNow().UtcDateTime;
    private IQueryable<Lot> Lots => db.Set<Lot>().AsNoTracking().Where(x => x.FabId == fabId);
    private IQueryable<Equipment> Equipment => EquipmentResources.EquipmentScope(db, fabId).AsNoTracking();

    internal async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest r)
    {
        var parts = r.Path.Split('/');
        if (r.Method == "GET" && parts.Length == 2)
        {
            if (parts[1] == "live-access")
                return new(WorkbenchAccess.Sources.Where(can).ToArray());
            if (!WorkbenchAccess.Sources.Any(can))
                require(WorkbenchAccess.Execution);
            return parts[1] switch
            {
                "options" => new(await Options()),
                "overview" => new(await Overview(r.Query)),
                "plant-board" => new(await PlantBoard(r.Query)),
                "records" => new(await Records(r.Query)),
                "history" => new(await History(r.Query)),
                "shifts" => new(await Shifts(r.Query)),
                "schedule" => new(await Schedule(r.Query)),
                "alerts" => new(await Alerts(r.Query)),
                "subscriptions" => new(await Subscriptions()),
                _ => throw Missing()
            };
        }

        if (r.Method == "GET" && parts.Length == 3 && parts[1] == "shift-monitor")
            return new(await ShiftMonitor(Id(parts[2]), r.Query));
        if (r.Method == "GET" && parts.Length == 4 && parts[1] == "schedule" && parts[3] == "history")
            return new(await PlanHistory(Id(parts[2])));
        if (parts[1] == "schedule")
            return await ChangeSchedule(r, parts);
        if (parts[1] == "subscriptions" && parts.Length == 3 && r.Method == "PUT")
            return await ChangeSubscription(parts[2], Body<WorkbenchSubscriptionInput>(r));
        if (parts[1] == "alerts" && parts.Length == 4 && r.Method == "PUT")
            return await ChangeAlert(parts[2], Id(parts[3]), Body<WorkbenchAlertInput>(r));
        if (parts[1] == "rates" && parts.Length == 2 && r.Method == "POST")
            return await AddRate(Body<AddWorkbenchRate>(r));
        throw Missing();
    }

    private static AppException Invalid(string message) => new(400, "InvalidWorkbenchInput", message);
    private static AppException Conflict(string message) => new(409, "WorkbenchConflict", message);
    private static AppException Missing() => new(404, "ResourceNotFound", "记录不存在或不可访问");

    private static T Body<T>(OrganizationIdentityAccessControlRequest r) where T : class =>
        r.Body.Deserialize<T>(Json) ?? throw Invalid("缺少请求内容");

    private static Guid Id(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty
        ? id : throw Invalid("无效 UUID");

    private static Guid? FilterId(IReadOnlyDictionary<string, string> query, string name) =>
        query.TryGetValue(name, out var value) && value != "" ? Id(value) : null;

    private static int Number(IReadOnlyDictionary<string, string> query, string name, int fallback, int max)
    {
        if (!query.TryGetValue(name, out var value) || value == "")
            return fallback;
        if (!int.TryParse(value, out var number) || number < 1 || number > max)
            throw Invalid(name + " 超出范围");
        return number;
    }

    private static string Tag(uint version) =>
        "\"" + Convert.ToBase64String(Encoding.UTF8.GetBytes(version.ToString(CultureInfo.InvariantCulture))) + "\"";

    private static void Match(string? expected, uint actual)
    {
        if (expected == null)
            throw new AppException(428, "VersionRequired", "请先读取排程版本");
        if (expected != Tag(actual))
            throw new AppException(412, "VersionConflict", "排程已修改，请刷新后重试");
    }

    private async Task<PageView<T>> Page<T>(IQueryable<T> rows, IReadOnlyDictionary<string, string> query)
    {
        var page = Number(query, "page", 1, 1000000);
        var size = Number(query, "pageSize", 20, 100);
        var total = await rows.CountAsync();
        return new(await rows.Skip((page - 1) * size).Take(size).ToArrayAsync(), total, page, size);
    }

    private async Task RequireActiveFab()
    {
        if (!await db.Set<Fab>().AnyAsync(x => x.Id == fabId && x.IsActive))
            throw Conflict("厂区已停用，仅可查询历史");
    }

    private async Task<TimeZoneInfo> Zone() => TimeZoneInfo.FindSystemTimeZoneById(await db.Set<Fab>()
        .Where(x => x.Id == fabId).Select(x => x.Timezone).SingleAsync());

    private DateOnly Today(TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(Now, zone));

    internal static DateTime Utc(DateOnly date, TimeOnly time, TimeZoneInfo zone)
    {
        var local = date.ToDateTime(time);
        if (zone.IsInvalidTime(local))
        {
            do
            {
                local = local.AddMinutes(1);
            } while (zone.IsInvalidTime(local));
        }
        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }

    private static DateOnly Date(IReadOnlyDictionary<string, string> query, string key, DateOnly fallback)
    {
        if (!query.TryGetValue(key, out var value) || value == "")
            return fallback;
        if (!DateOnly.TryParseExact(
                value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || date.Year is < 1900 or > 2200)
            throw Invalid("日期须为 YYYY-MM-DD");
        return date;
    }

    private static DateOnly[] Days(DateOnly from, DateOnly to, int max)
    {
        var count = to.DayNumber - from.DayNumber + 1;
        if (count < 1 || count > max || from.Year < 1900 || to.Year > 2200)
            throw Invalid($"日期范围须为 1～{max} 天");
        return Enumerable.Range(0, count).Select(from.AddDays).ToArray();
    }

    private void Audit(Guid id, string type, string action, object? before, object? after) =>
        db.Add(new AuditLog
        {
            FabId = fabId,
            Module = "workbench",
            EntityId = id,
            EntityType = type,
            Action = action,
            UserId = actor.UserId,
            OccurredAt = Now,
            CorrelationId = actor.CorrelationId,
            SourceChannel = "Web",
            BeforeState = before == null ? null : JsonSerializer.Serialize(before, Json),
            AfterState = after == null ? null : JsonSerializer.Serialize(after, Json)
        });
}
