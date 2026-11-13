using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.LotWaferCarrierGenealogy;

public sealed partial class ProductionBackend
{
    private async Task Location(Guid id)
    {
        var valid = await (from location in db.Set<Location>() join area in db.Set<Area>()
                           on location.AreaId equals area.Id
                           where location.Id == id && location.IsActive && area.IsActive && area.FabId == fabId
                           select location.Id).AnyAsync();
        if (!valid)
            throw Error("ResourceNotFound", "位置不存在、停用或不属于当前厂区", 404);
    }

    private async Task TouchCarriers(IEnumerable<Guid> waferIds)
    {
        var ids = waferIds.ToArray();
        var carriers = await (from load in db.Set<CarrierLoad>() join slot in db.Set<CarrierSlot>()
                              on load.CarrierSlotId equals slot.Id join carrier in db.Set<Carrier>()
                              on slot.CarrierId equals carrier.Id
                              where ids.Contains(load.WaferId) && load.UnloadedAt == null
                              select carrier).Distinct().ToListAsync();
        foreach (var carrier in carriers)
        {
            if (carrier.FabId != fabId)
                throw Error("MembershipConflict", "载具与晶圆厂区不一致", 409);
            Touch(carrier);
        }
    }

    private async Task<OrganizationIdentityAccessControlResult> CreateCarrier(CreateProductionCarrier input)
    {
        if (input.SlotCapacity is < 1 or > 100 || input.AllowedWaferSizeMm <= 0
            || input.AllowedWaferSizeMm != decimal.Round(input.AllowedWaferSizeMm, 6))
            throw Error("InvalidRequest", "容量须为 1～100，尺寸须为正数且最多六位小数");
        if (input.CurrentLocationId is Guid location)
            await Location(location);
        var carrier = new Carrier
        {
            FabId = fabId, CarrierCode = Text(input.CarrierCode, 64), CarrierType = Text(input.CarrierType, 32),
            SlotCapacity = input.SlotCapacity, AllowedWaferSizeMm = input.AllowedWaferSizeMm,
            CurrentLocationId = input.CurrentLocationId, Status = "Available"
        };
        db.Add(carrier);
        for (var number = 1; number <= carrier.SlotCapacity; number++)
            db.Add(new CarrierSlot { CarrierId = carrier.Id, SlotNo = number });
        Record(carrier, "CarrierCreated", new { carrier.CarrierCode, carrier.SlotCapacity, carrier.CurrentLocationId });
        await db.SaveChangesAsync();
        return new(View(carrier), Tag(carrier.RowVersion), 201,
            Location: $"/api/v1/production/carriers/{carrier.Id}");
    }

    private async Task<List<CarrierLoad>> CarrierLoads(Guid id) => await (from load in db.Set<CarrierLoad>()
        join slot in db.Set<CarrierSlot>() on load.CarrierSlotId equals slot.Id
        where slot.CarrierId == id && load.UnloadedAt == null select load).ToListAsync();

    private async Task<OrganizationIdentityAccessControlResult> UpdateCarrier(Carrier carrier, UpdateProductionCarrier input)
    {
        if (input.Status is not ("Available" or "Disabled"))
            throw Error("InvalidRequest", "载具状态无效");
        if (input.Status == "Disabled" && (await CarrierLoads(carrier.Id)).Count != 0)
            throw Error("ResourceInUse", "有在载晶圆，不能停用", 409);
        var before = carrier.Status;
        carrier.Status = input.Status;
        Touch(carrier);
        Record(carrier, "CarrierUpdated", new { before, carrier.Status });
        await db.SaveChangesAsync();
        return new(View(carrier), Tag(carrier.RowVersion));
    }

    private async Task<Lot> MovableWafer(Wafer wafer)
    {
        if (wafer.LifecycleStatus is not ("Registered" or "Active" or "Completed"))
            throw Error("LotStateConflict", "该晶圆不能进行普通载具操作", 409);
        var member = await db.Set<LotMembership>().SingleOrDefaultAsync(x => x.WaferId == wafer.Id && x.ValidTo == null)
            ?? throw Error("MembershipConflict", "晶圆没有有效批次归属", 409);
        var lot = await Lot(member.LotId);
        if (lot.WorkOrderId != wafer.WorkOrderId || LotRules.IsTerminal(lot.Status) && lot.Status != "Completed")
            throw Error("MembershipConflict", "晶圆与批次状态或工单不一致", 409);
        await Unblocked(lot);
        return lot;
    }

    private async Task<OrganizationIdentityAccessControlResult> MoveCarrier(Carrier carrier, MoveProductionCarrier input)
    {
        var reason = Text(input.Reason, 1000);
        if (carrier.Status != "Available" || carrier.CurrentLocationId == input.ToLocationId)
            throw Error("LotStateConflict", "载具不可用或目标与当前位置相同", 409);
        await Location(input.ToLocationId);
        foreach (var load in await CarrierLoads(carrier.Id))
            await MovableWafer(await Wafer(load.WaferId));
        var old = carrier.CurrentLocationId;
        var movement = new CarrierMovement
        {
            CarrierId = carrier.Id, FromLocationId = old, ToLocationId = input.ToLocationId,
            MovedAt = Now, PerformedById = actor.UserId, Notes = reason
        };
        db.Add(movement);
        carrier.CurrentLocationId = input.ToLocationId;
        Record(carrier, "CarrierMoved", new { movement.Id, old, input.ToLocationId, reason });
        await db.SaveChangesAsync();
        return new(View(carrier), Tag(carrier.RowVersion));
    }

    private static void Versions(ProductionVersion[]? supplied, Dictionary<Guid, uint> expected)
    {
        if (supplied is null || supplied.Any(x => x is null) || supplied.Any(x => string.IsNullOrEmpty(x.Etag)))
            throw Error("VersionRequired", "缺少资源版本", 428);
        if (supplied.Length != expected.Count || supplied.Select(x => x.Id).Distinct().Count() != supplied.Length
            || !supplied.Select(x => x.Id).ToHashSet().SetEquals(expected.Keys))
            throw Error("VersionRequired", "版本必须完整覆盖相关资源且不得重复或包含无关对象", 428);
        foreach (var version in supplied)
            Match(version.Etag, expected[version.Id]);
    }

    private async Task<OrganizationIdentityAccessControlResult> Loads(ChangeCarrierLoads input)
    {
        var reason = Text(input.Reason, 1000);
        if (input.Moves is null || input.Moves.Length is < 1 or > 100 || input.Moves.Any(x => x is null)
            || input.Moves.Select(x => x.WaferId).Distinct().Count() != input.Moves.Length
            || input.Moves.Any(x => x.FromSlotId == x.ToSlotId)
            || input.Moves.Where(x => x.ToSlotId != null).Select(x => x.ToSlotId).Distinct().Count()
                != input.Moves.Count(x => x.ToSlotId != null))
            throw Error("InvalidWaferSet", "装载清单无效、片/目标槽重复或没有位置变化");
        var wafers = new Dictionary<Guid, Wafer>();
        var lots = new Dictionary<Guid, Lot>();
        var carriers = new Dictionary<Guid, Carrier>();
        var closing = new List<CarrierLoad>();
        var movingIds = input.Moves.Select(x => x.WaferId).ToArray();
        foreach (var move in input.Moves)
        {
            var wafer = await Wafer(move.WaferId);
            wafers.Add(wafer.Id, wafer);
            var lot = await MovableWafer(wafer);
            lots[lot.Id] = lot;
            var current = await db.Set<CarrierLoad>().SingleOrDefaultAsync(x => x.WaferId == wafer.Id
                && x.UnloadedAt == null);
            if (current?.CarrierSlotId != move.FromSlotId)
                throw Error("MembershipConflict", "预期原槽与当前装载不一致", 409);
            if (current != null)
                closing.Add(current);
            foreach (var slotId in new[] { move.FromSlotId, move.ToSlotId }.Where(x => x != null).Distinct())
            {
                var slot = await db.Set<CarrierSlot>().SingleOrDefaultAsync(x => x.Id == slotId)
                    ?? throw Error("ResourceNotFound", "槽位不存在", 404);
                var carrier = await Carrier(slot.CarrierId);
                carriers[carrier.Id] = carrier;
                if (slotId == move.ToSlotId)
                {
                    if (carrier.Status != "Available" || carrier.AllowedWaferSizeMm != wafer.WaferSizeMm
                        || slot.SlotNo > carrier.SlotCapacity)
                        throw Error("CarrierSizeMismatch", "目标载具不可用、规格未登记或尺寸不兼容");
                    var occupant = await db.Set<CarrierLoad>().SingleOrDefaultAsync(x =>
                        x.CarrierSlotId == slotId && x.UnloadedAt == null);
                    if (occupant != null && !movingIds.Contains(occupant.WaferId))
                        throw Error("SlotOccupied", "目标槽已被其它晶圆占用", 409);
                }
            }
        }
        Versions(input.CarrierVersions, carriers.ToDictionary(x => x.Key, x => x.Value.RowVersion));
        Versions(input.WaferVersions, wafers.ToDictionary(x => x.Key, x => x.Value.RowVersion));
        Versions(input.LotVersions, lots.ToDictionary(x => x.Key, x => x.Value.RowVersion));
        var time = LotRules.NextEventTime(Now, closing.Select(x => x.LoadedAt));
        foreach (var load in closing)
            load.UnloadedAt = time;
        await db.SaveChangesAsync();
        var opened = new List<CarrierLoad>();
        foreach (var move in input.Moves.Where(x => x.ToSlotId != null))
        {
            var load = new CarrierLoad { WaferId = move.WaferId, CarrierSlotId = move.ToSlotId!.Value, LoadedAt = time };
            opened.Add(load);
            db.Add(load);
        }
        foreach (var wafer in wafers.Values)
        {
            Touch(wafer);
            Record(wafer, "WaferLoadChanged",
                new { reason, move = input.Moves.Single(x => x.WaferId == wafer.Id), time });
        }
        foreach (var lot in lots.Values)
            Touch(lot);
        foreach (var carrier in carriers.Values)
        {
            Touch(carrier);
            Record(carrier, "CarrierLoadsChanged", new
            {
                reason, moves = input.Moves,
                    closedLoadIds = closing.Select(x => x.Id), newLoadIds = opened.Select(x => x.Id)
            });
        }
        await db.SaveChangesAsync();
        return new(new
        {
            commandId, carriers = carriers.Values.Select(View), wafers = wafers.Values.Select(View),
            lots = lots.Values.Select(View),
                closedLoadIds = closing.Select(x => x.Id), newLoadIds = opened.Select(x => x.Id)
        });
    }
}
