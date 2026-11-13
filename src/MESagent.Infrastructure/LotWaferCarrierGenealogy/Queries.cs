using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.LotWaferCarrierGenealogy;

public sealed partial class ProductionBackend
{
    private static (int Page, int Size) Paging(OrganizationIdentityAccessControlRequest r)
    {
        var page = r.Query.TryGetValue("page", out var p) && int.TryParse(p, out var n) ? n : 1;
        var size = r.Query.TryGetValue("pageSize", out var s) && int.TryParse(s, out n) ? n : 20;
        if (page < 1 || page > 1000000 || size < 1 || size > 100)
            throw Error("InvalidRequest", "分页参数无效");
        return (page, size);
    }

    private async Task<object> Page<T>(IQueryable<T> query, OrganizationIdentityAccessControlRequest r,
        Func<T, Task<object>>? project = null) where T : class
    {
        var (page, size) = Paging(r);
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => EF.Property<Guid>(x, "Id"))
            .Skip((page - 1) * size).Take(size).ToListAsync();
        var items = new List<object>();
        foreach (var row in rows)
            items.Add(project == null ? View(row) : await project(row));
        return new { items, total, page, pageSize = size };
    }

    private async Task<object> LotView(Lot lot)
    {
        var members = await Members(lot.Id);
        var ids = members.Select(x => x.WaferId).ToArray();
        var wafers = await db.Set<Wafer>().Where(x => ids.Contains(x.Id)).ToListAsync();
        var blocks = await Blocks(lot);
        var result = View(lot);
        result["workOrderNo"] = (await Order(lot.WorkOrderId!.Value, false)).OrderNo;
        result["productRevisionCode"] = await db.Set<ProductRevision>().Where(x => x.Id == lot.ProductRevisionId)
            .Select(x => x.RevisionCode).SingleAsync();
        result["quantities"] = new
        {
            currentWaferCount = members.Count,
            releasedQty = await db.Set<WaferRelease>().CountAsync(x => ids.Contains(x.WaferId)),
            inProcessQty = wafers.Count(x => x.LifecycleStatus == "Active"),
            completedQty = (int?)null,
            scrapQty = await db.Set<WaferDisposition>().CountAsync(x => x.LotIdAtEvent == lot.Id
                && x.DispositionType == "Scrap")
        };
        result["blockingReasons"] = blocks;
        result["activeHoldCount"] = await db.Set<HoldRecord>()
            .CountAsync(x => x.LotId == lot.Id && x.ReleasedAt == null);
        result["currentStep"] = await db.Set<StepExecution>()
            .SingleOrDefaultAsync(x => x.LotId == lot.Id && x.ExitedAt == null);
        result["coverage"] = "PendingIntegration";
        result["missingDependencies"] = new[] { "F5 真实执行与终点完成", "F6 完整质量处置" };
        result["asOf"] = Now;
        result["allowedActions"] = new[]
        {
            ("manage", lot.Status == "Draft"), ("release", lot.Status == "Draft" && blocks.Length == 0),
            ("cancel", lot.Status == "Draft" && blocks.Length == 0),
            ("split", lot.Status == "Ready" && blocks.Length == 0),
            ("merge", lot.Status == "Ready" && blocks.Length == 0)
        }.Where(x => x.Item2 && can("production.lots." + x.Item1)).Select(x => x.Item1).ToArray();
        return result;
    }

    private async Task<object> WaferView(Wafer wafer)
    {
        var view = View(wafer);
        view["currentMembership"] = await db.Set<LotMembership>().SingleOrDefaultAsync(x =>
            x.WaferId == wafer.Id && x.ValidTo == null);
        var member = await db.Set<LotMembership>().SingleOrDefaultAsync(x => x.WaferId == wafer.Id && x.ValidTo == null);
        view["currentLotNumber"] = member == null ? null : (await Lot(member.LotId)).LotNumber;
        view["workOrderNo"] = (await Order(wafer.WorkOrderId!.Value, false)).OrderNo;
        var load = await db.Set<CarrierLoad>().SingleOrDefaultAsync(x => x.WaferId == wafer.Id && x.UnloadedAt == null);
        view["currentLoad"] = load;
        view["currentSlot"] = load == null ? null : await db.Set<CarrierSlot>().SingleAsync(x => x.Id == load.CarrierSlotId);
        view["release"] = await db.Set<WaferRelease>().SingleOrDefaultAsync(x => x.WaferId == wafer.Id);
        view["completion"] = await db.Set<WaferCompletion>().SingleOrDefaultAsync(x => x.WaferId == wafer.Id);
        view["sourceCoverage"] = wafer.OriginMaterialLotId == null ? "Unregistered" : "Referenced";
        return view;
    }

    private async Task<object> CarrierView(Carrier carrier)
    {
        var view = View(carrier);
        view["currentLocationName"] = carrier.CurrentLocationId == null ? null
            : await db.Set<Location>().Where(x => x.Id == carrier.CurrentLocationId).Select(x => x.Name).SingleAsync();
        var slots = await db.Set<CarrierSlot>()
            .Where(x => x.CarrierId == carrier.Id).OrderBy(x => x.SlotNo).ToListAsync();
        var loads = await CarrierLoads(carrier.Id);
        var items = new List<object>();
        foreach (var slot in slots)
        {
            var load = loads.SingleOrDefault(x => x.CarrierSlotId == slot.Id);
            items.Add(new
            {
                slot.Id, slot.SlotNo, occupied = load != null,
                wafer = load != null && can("production.wafers.read") ? await WaferView(await Wafer(load.WaferId)) : null
            });
        }
        view["slots"] = items;
        view["allowedActions"] = new[] { "manage", "load", "move" }
            .Where(x => can("production.carriers." + x)).ToArray();
        return view;
    }

    private async Task<object> Query(OrganizationIdentityAccessControlRequest r, string[] p)
    {
        var q = r.Query;
        if (p[1] == "lots")
        {
            if (p.Length == 2)
            {
                var lots = db.Set<Lot>()
                    .Where(x => x.FabId == fabId && x.WorkOrderId != null && x.SnapshotVersion != null);
                if (q.TryGetValue("lotNumber", out var number))
                    lots = lots.Where(x => x.LotNumber.Contains(number));
                if (q.TryGetValue("status", out var status))
                    lots = lots.Where(x => x.Status == status);
                if (q.TryGetValue("workOrderId", out var order))
                {
                    var id = Id(order);
                    lots = lots.Where(x => x.WorkOrderId == id);
                }
                if (q.TryGetValue("routeStepId", out var step))
                {
                    var id = Id(step);
                    lots = lots.Where(x => x.CurrentRouteStepId == id);
                }
                if (q.TryGetValue("hasHold", out var held) && bool.TryParse(held, out var hasHold))
                    lots = lots.Where(x => db.Set<HoldRecord>().Any(h => h.LotId == x.Id && h.ReleasedAt == null) == hasHold);
                return await Page(lots, r, LotView);
            }
            var lot = await Lot(Id(p[2]));
            if (p.Length == 3)
                return await LotView(lot);
            if (p.Length == 4 && p[3] == "wafers")
            {
                require("production.wafers.read");
                if (q.TryGetValue("includeHistory", out var history) && history == "true")
                    return await Page(db.Set<LotMembership>().Where(x => x.LotId == lot.Id), r);
                var ids = (await Members(lot.Id)).Select(x => x.WaferId).ToArray();
                return await Page(db.Set<Wafer>().Where(x => ids.Contains(x.Id) && x.FabId == fabId), r, WaferView);
            }
            if (p.Length == 4 && p[3] == "genealogy")
            {
                require("production.lots.read");
                return await GenealogyQuery(lot.Id, r);
            }
        }
        if (p[1] == "wafers")
        {
            if (p.Length == 2)
            {
                var wafers = db.Set<Wafer>().Where(x => x.FabId == fabId && x.WorkOrderId != null);
                if (q.TryGetValue("waferCode", out var code))
                    wafers = wafers.Where(x => x.WaferCode.Contains(code));
                if (q.TryGetValue("workOrderId", out var order))
                {
                    var id = Id(order);
                    wafers = wafers.Where(x => x.WorkOrderId == id);
                }
                if (q.TryGetValue("lotId", out var lot))
                {
                    var id = (await Lot(Id(lot))).Id;
                    wafers = wafers.Where(x => db.Set<LotMembership>().Any(m => m.LotId == id && m.WaferId == x.Id && m.ValidTo == null));
                }
                if (q.TryGetValue("lifecycleStatus", out var status))
                    wafers = wafers.Where(x => x.LifecycleStatus == status);
                return await Page(wafers, r, WaferView);
            }
            var wafer = await Wafer(Id(p[2]));
            if (p.Length == 3)
                return await WaferView(wafer);
            if (p.Length == 4 && p[3] == "history")
                return await WaferHistory(wafer, r);
        }
        if (p[1] == "carriers")
        {
            if (p.Length == 2)
            {
                var carriers = db.Set<Carrier>().Where(x => x.FabId == fabId);
                if (q.TryGetValue("carrierCode", out var code))
                    carriers = carriers.Where(x => x.CarrierCode.Contains(code));
                if (q.TryGetValue("status", out var status))
                    carriers = carriers.Where(x => x.Status == status);
                if (q.TryGetValue("locationId", out var location))
                {
                    var id = Id(location);
                    carriers = carriers.Where(x => x.CurrentLocationId == id);
                }
                return await Page(carriers, r);
            }
            var carrier = await Carrier(Id(p[2]));
            if (p.Length == 3)
                return await CarrierView(carrier);
            if (p.Length == 4 && p[3] == "history")
                return await CarrierHistory(carrier, r);
        }
        if (p.Length == 3 && p[1] == "genealogy-events")
            return await GenealogyView(Id(p[2]));
        throw Error("ResourceNotFound", "接口不存在", 404);
    }

    private async Task<object> GenealogyView(Guid id)
    {
        var details = await db.Set<GenealogyDetail>().Where(x => x.EventId == id).ToListAsync();
        if (details.Count == 0)
            throw Error("ResourceNotFound", "血缘事件不存在", 404);
        var lotNumbers = new Dictionary<Guid, string>();
        foreach (var lotId in details.Select(x => x.ToLotId).Concat(details.Where(x => x.FromLotId != null)
            .Select(x => x.FromLotId!.Value)).Distinct())
            lotNumbers[lotId] = (await Lot(lotId)).LotNumber;
        var evt = await db.Set<GenealogyEvent>().SingleAsync(x => x.Id == id);
        return new
        {
            evt.Id, evt.EventType, evt.Reason, evt.OccurredAt, evt.PerformedById, details, lotNumbers,
            inheritances = await db.Set<StepExecutionInheritance>().Where(x => x.GenealogyEventId == id).ToListAsync()
        };
    }

    private async Task<object> GenealogyQuery(Guid lotId, OrganizationIdentityAccessControlRequest r)
    {
        var direction = r.Query.GetValueOrDefault("direction", "both");
        if (direction is not ("both" or "ancestors" or "descendants"))
            throw Error("InvalidRequest", "血缘方向无效");
        var depth = r.Query.TryGetValue("depth", out var text) && int.TryParse(text, out var d) ? d : 1;
        if (depth is < 1 or > 10)
            throw Error("InvalidRequest", "血缘深度须为 1～10");
        var seen = new HashSet<Guid>();
        var frontier = new HashSet<Guid> { lotId };
        var events = new HashSet<Guid>();
        var truncated = false;
        for (var level = 0; level < depth && frontier.Count > 0; level++)
        {
            seen.UnionWith(frontier);
            var ids = frontier.ToArray();
            var details = await db.Set<GenealogyDetail>().Where(x =>
                direction != "descendants" && ids.Contains(x.ToLotId)
                || direction != "ancestors" && x.FromLotId != null && ids.Contains(x.FromLotId.Value))
                .OrderBy(x => x.Id).Take(1001).ToListAsync();
            truncated |= details.Count > 1000;
            details = details.Take(1000).ToList();
            events.UnionWith(details.Select(x => x.EventId));
            frontier = details.Select(x => x.ToLotId).Concat(details.Where(x => x.FromLotId != null)
                .Select(x => x.FromLotId!.Value)).Where(x => !seen.Contains(x)).ToHashSet();
        }
        truncated |= frontier.Count > 0;
        var (page, size) = Paging(r);
        var ordered = await db.Set<GenealogyEvent>().Where(x => events.Contains(x.Id))
            .OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).Skip((page - 1) * size).Take(size).ToListAsync();
        var items = new List<object>();
        foreach (var evt in ordered)
            items.Add(await GenealogyView(evt.Id));
        return new { items, total = events.Count, page,
            pageSize = size, truncated, hasMore = events.Count > page * size || truncated };
    }

    private sealed record HistoryItem(Guid Id, string Type, DateTime OccurredAt, object Data);

    private object HistoryPage(IEnumerable<HistoryItem> records, OrganizationIdentityAccessControlRequest r, bool truncated)
    {
        if (r.Query.TryGetValue("type", out var type))
            records = records.Where(x => x.Type == type);
        foreach (var key in new[] { "from", "to" })
            if (r.Query.TryGetValue(key, out var text))
            {
                if (!DateTimeOffset.TryParse(text, out var time))
                    throw Error("InvalidRequest", "历史时间筛选无效");
                records = key == "from" ? records.Where(x => x.OccurredAt >= time.UtcDateTime)
                    : records.Where(x => x.OccurredAt < time.UtcDateTime);
            }
        var sorted = records.OrderBy(x => x.OccurredAt).ThenBy(x => x.Id).ThenBy(x => x.Type).ToArray();
        var (page, size) = Paging(r);
        return new
        {
            items = sorted.Skip((page - 1) * size).Take(size), total = sorted.Length, page, pageSize = size,
            truncated, coverage = "PendingIntegration", missingDependencies = new[] { "F5/F6 完整生产履历" }, asOf = Now
        };
    }

    private async Task<object> WaferHistory(Wafer wafer, OrganizationIdentityAccessControlRequest r)
    {
        var memberships = await db.Set<LotMembership>().Where(x => x.WaferId == wafer.Id)
            .OrderBy(x => x.ValidFrom).Take(1001).ToListAsync();
        var loads = await db.Set<CarrierLoad>().Where(x => x.WaferId == wafer.Id)
            .OrderBy(x => x.LoadedAt).Take(1001).ToListAsync();
        var records = new List<HistoryItem>();
        foreach (var member in memberships.Take(1000))
        {
            await Lot(member.LotId);
            records.Add(new(member.Id, "Membership", member.ValidFrom, new
            {
                member.LotId, lotNumber = (await Lot(member.LotId)).LotNumber,
                member.ValidFrom, member.ValidTo, member.ChangeEventId
            }));
        }
        foreach (var load in loads.Take(1000))
        {
            records.Add(new(load.Id, "Loaded", load.LoadedAt, load));
            if (load.UnloadedAt is DateTime time)
                records.Add(new(load.Id, "Unloaded", time, load));
        }
        if (can("production.genealogy.read"))
            foreach (var eventId in memberships.Take(1000).Select(x => x.ChangeEventId).Distinct())
            {
                var evt = await db.Set<GenealogyEvent>().SingleAsync(x => x.Id == eventId);
                records.Add(new(evt.Id, "Genealogy", evt.OccurredAt, await GenealogyView(evt.Id)));
            }
        return HistoryPage(records, r, memberships.Count > 1000 || loads.Count > 1000);
    }

    private async Task<object> CarrierHistory(Carrier carrier, OrganizationIdentityAccessControlRequest r)
    {
        var moves = await db.Set<CarrierMovement>().Where(x => x.CarrierId == carrier.Id)
            .OrderBy(x => x.MovedAt).Take(1001).ToListAsync();
        var records = moves.Take(1000).Select(x => new HistoryItem(x.Id, "Movement", x.MovedAt, x)).ToList();
        var loads = await (from load in db.Set<CarrierLoad>() join slot in db.Set<CarrierSlot>()
                           on load.CarrierSlotId equals slot.Id where slot.CarrierId == carrier.Id select load)
            .OrderBy(x => x.LoadedAt).Take(1001).ToListAsync();
        foreach (var load in loads.Take(1000))
        {
            object data = can("production.wafers.read") ? load : new { load.Id, load.CarrierSlotId };
            records.Add(new(load.Id, "Loaded", load.LoadedAt, data));
            if (load.UnloadedAt is DateTime time)
                records.Add(new(load.Id, "Unloaded", time, data));
        }
        return HistoryPage(records, r, moves.Count > 1000 || loads.Count > 1000);
    }
}
