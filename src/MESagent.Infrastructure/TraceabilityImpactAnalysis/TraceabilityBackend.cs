using System.Globalization;
using System.Text.RegularExpressions;
using MESagent.Application;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.MaterialConsumptionInventory;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.TraceabilityImpactAnalysis;

internal sealed partial class TraceabilityBackend(MesDbContext db, Guid fabId, Action<string> require,
    Func<string, bool> can, TimeProvider clock)
{
    private const string OrderRead = "planning.work-orders.read";
    private const string LotRead = "production.lots.read";
    private const string WaferRead = "production.wafers.read";
    private const string GenealogyRead = "production.genealogy.read";
    private const string CarrierRead = "production.carriers.read";
    private const string EquipmentRead = "equipment.read";
    private const string ExecutionRead = "execution.read";
    private const string QualityRead = "quality.read";

    private IQueryable<WorkOrder> Orders => db.Set<WorkOrder>().AsNoTracking().Where(x => x.FabId == fabId);
    private IQueryable<Lot> Lots => db.Set<Lot>().AsNoTracking().Where(x => x.FabId == fabId);
    private IQueryable<Wafer> Wafers => db.Set<Wafer>().AsNoTracking().Where(x => x.FabId == fabId);
    private IQueryable<Carrier> Carriers => db.Set<Carrier>().AsNoTracking().Where(x => x.FabId == fabId);
    private IQueryable<Equipment> Equipment => db.Set<Equipment>().AsNoTracking().Where(x =>
        db.Set<Area>().Any(area => area.Id == x.AreaId && area.FabId == fabId));
    private IQueryable<Location> Locations => db.Set<Location>().Where(x =>
        db.Set<Area>().Any(area => area.Id == x.AreaId && area.FabId == fabId));

    private static string Permission(string kind) => kind switch
    {
        "work-order" => OrderRead,
        "lot" => LotRead,
        "wafer" => WaferRead,
        "carrier" => CarrierRead,
        "equipment" => EquipmentRead,
        _ => throw Invalid("追溯对象类型无效")
    };

    private static AppException Invalid(string message) => new(400, "InvalidQuery", message);

    private static AppException NotFound() => new(404, "ResourceNotFound", "追溯对象或记录不存在");

    private static int PageValue(IReadOnlyDictionary<string, string> query, string key, int fallback, int max)
    {
        if (!query.TryGetValue(key, out var text))
            return fallback;
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && number >= 1 && number <= max ? number : throw Invalid($"{key} 超出分页范围");
    }

    private static Guid Id(string text) => Guid.TryParse(text, out var id) ? id : throw NotFound();

    private IQueryable<TraceObject> Subjects(string kind) => kind switch
    {
        "work-order" => Orders.Select(x => new TraceObject
            { Id = x.Id, Kind = kind, Code = x.OrderNo, Status = x.Status }),
        "lot" => Lots.Select(x => new TraceObject
            { Id = x.Id, Kind = kind, Code = x.LotNumber, Status = x.Status }),
        "wafer" => Wafers.Select(x => new TraceObject
            { Id = x.Id, Kind = kind, Code = x.WaferCode, Status = x.LifecycleStatus }),
        "carrier" => Carriers.Select(x => new TraceObject
            { Id = x.Id, Kind = kind, Code = x.CarrierCode, Status = x.Status }),
        _ => Equipment.Select(x => new TraceObject
        {
            Id = x.Id,
            Kind = kind,
            Code = x.Code,
            Name = x.Name,
            Status = x.IsActive
                ? db.Set<EquipmentState>().Where(s => s.EquipmentId == x.Id && s.EndedAt == null)
                    .Select(s => s.State).FirstOrDefault() ?? x.OperationalState
                : "Disabled"
        })
    };

    private async Task<TraceObject> Subject(string kind, Guid id) =>
        await Subjects(kind).SingleOrDefaultAsync(x => x.Id == id) ?? throw NotFound();

    public async Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest request)
    {
        var path = request.Path.Split('/');
        if (request.Method != "GET")
            throw NotFound();
        if (path is [_, "search"])
            return new(await Search(request.Query));
        if (path.Length < 3)
            throw NotFound();

        var kind = path[1];
        require(Permission(kind));
        var subject = await Subject(kind, Id(path[2]));
        var groups = Groups(kind);
        return path switch
        {
            [_, _, _] => new(await Overview(subject, groups)),
            [_, _, _, "events"] => new(await Timeline(subject, groups, request.Query)),
            [_, _, _, "events", var type, var source] => new(await Evidence(subject, groups, type, Id(source))),
            _ => throw NotFound()
        };
    }

    private async Task<PageView<TraceObject>> Search(IReadOnlyDictionary<string, string> query)
    {
        var kind = query.GetValueOrDefault("kind", "");
        require(Permission(kind));
        var code = query.GetValueOrDefault("q", "").Trim();
        if (code.Length is < 1 or > 120)
            throw Invalid("请输入 1～120 字符的编号");
        var pattern = "%" + code.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        var page = PageValue(query, "page", 1, 1000000);
        var size = PageValue(query, "pageSize", 20, 100);
        var matches = Subjects(kind).Where(x => EF.Functions.ILike(x.Code, pattern, "\\"));
        var total = await matches.CountAsync();
        var items = await matches.OrderBy(x => x.Code).ThenBy(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToArrayAsync();
        return new(items, total, page, size);
    }

    private string[] Groups(string kind)
    {
        var production = kind is "work-order" or "lot" or "wafer";
        var result = new List<string>();
        if (kind == "work-order")
            result.Add("orders");
        if ((kind is "work-order" or "lot") && can(LotRead))
            result.Add("lots");
        if (production && can(WaferRead))
            result.Add("wafers");
        if (production && can(LotRead) && can(WaferRead) && can(GenealogyRead))
            result.Add("genealogy");
        if ((production || kind == "carrier") && can(CarrierRead))
            result.Add("carriers");
        if (can(ExecutionRead))
            result.Add("execution");
        if ((production || kind == "equipment") && can(QualityRead))
            result.Add("quality");
        if (kind == "equipment")
            result.Add("equipment");
        return result.ToArray();
    }

    private async Task<TraceOverview> Overview(TraceObject subject, string[] groups)
    {
        var related = new List<TraceLink>();
        string? origin = null;
        if (subject.Kind == "lot" && can(OrderRead))
        {
            var orderLink = await (from lot in Lots
                               join order in Orders on lot.WorkOrderId equals order.Id
                               where lot.Id == subject.Id
                               select new TraceLink(order.Id, "work-order", order.OrderNo, "工单"))
                .SingleOrDefaultAsync();
            if (orderLink != null)
                related.Add(orderLink);
        }
        if (subject.Kind == "wafer")
        {
            var wafer = await Wafers.SingleAsync(x => x.Id == subject.Id);
            if (can(OrderRead) && wafer.WorkOrderId is Guid orderId)
            {
                var order = await Orders.Where(x => x.Id == orderId)
                    .Select(x => new TraceLink(x.Id, "work-order", x.OrderNo, "工单")).SingleOrDefaultAsync();
                if (order != null)
                    related.Add(order);
            }
            if (can(LotRead))
            {
                var lotLink = await (from member in Memberships
                                 join lot in Lots on member.LotId equals lot.Id
                                 where member.WaferId == wafer.Id && member.ValidTo == null
                                 select new TraceLink(lot.Id, "lot", lot.LotNumber, "当前批次"))
                    .SingleOrDefaultAsync();
                if (lotLink != null)
                    related.Add(lotLink);
            }
            if (can(MaterialBackend.ReadPermission))
            {
                var source = await WaferOrigins.Lots(db, fabId).Where(x => x.Lot.Id == wafer.OriginMaterialLotId)
                    .Select(x => new TraceLink(x.Lot.Id, "material-lot", x.Lot.LotNumber, "来料批"))
                    .SingleOrDefaultAsync();
                origin = source != null
                    ? "Registered"
                    : wafer.OriginMaterialLotId == null ? "Unregistered" : "Unavailable";
                if (source != null)
                    related.Add(source);
            }
            if (can(CarrierRead))
            {
                var carrierLink = await (from load in Loads
                                         join slot in Slots on load.CarrierSlotId equals slot.Id
                                         join carrier in Carriers on slot.CarrierId equals carrier.Id
                                         where load.WaferId == wafer.Id && load.UnloadedAt == null
                                         select new TraceLink(carrier.Id, "carrier", carrier.CarrierCode,
                                             "当前载具 · 槽位 " + slot.SlotNo.ToString()))
                    .SingleOrDefaultAsync();
                if (carrierLink != null)
                    related.Add(carrierLink);
            }
        }
        return new(subject, groups, related.ToArray(), origin, clock.GetUtcNow().UtcDateTime);
    }

    private static DateTime? TimeBound(IReadOnlyDictionary<string, string> query, string key)
    {
        if (!query.TryGetValue(key, out var text))
            return null;
        if (!Regex.IsMatch(text, @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?(Z|[+-]\d{2}:\d{2})$")
            || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            throw Invalid($"{key} 须为带时区的 ISO 8601 时间");
        return time.UtcDateTime;
    }

    private async Task<PageView<TraceEvent>> Timeline(
        TraceObject subject,
        string[] groups,
        IReadOnlyDictionary<string, string> query)
    {
        if (query.TryGetValue("group", out var group))
        {
            if (!groups.Contains(group))
                throw Invalid("此对象没有该履历类别");
            groups = [group];
        }
        var page = PageValue(query, "page", 1, 1000000);
        var size = PageValue(query, "pageSize", 20, 100);
        var from = TimeBound(query, "from");
        var to = TimeBound(query, "to");
        if (from >= to)
            throw Invalid("履历结束时间须晚于开始时间");

        var facts = Events(subject, groups);
        if (from is DateTime start)
            facts = facts.Where(x => x.OccurredAt >= start);
        if (to is DateTime end)
            facts = facts.Where(x => x.OccurredAt < end);
        var total = await facts.CountAsync();
        var rows = await facts.OrderByDescending(x => x.OccurredAt).ThenBy(x => x.Type).ThenBy(x => x.Id)
            .Skip((page - 1) * size).Take(size).ToArrayAsync();
        return new(await Render(rows), total, page, size);
    }
}
