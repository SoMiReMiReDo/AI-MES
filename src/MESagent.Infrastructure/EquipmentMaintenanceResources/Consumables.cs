using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.EquipmentMaintenanceResources;

public sealed partial class EquipmentBackend
{
    private async Task<OrganizationIdentityAccessControlResult> Consumables(
        OrganizationIdentityAccessControlRequest r, string[] p)
    {
        if (p.Length == 2 && r.Method == "POST")
        {
            if (p[1] == "reticles")
            {
                var input = Body<RegisterReticle>(r);
                if (input.MaxUsageCount < 1 || input.MaxUsageCount > 1000000000)
                    throw Error("InvalidReticleLife", "光罩寿命次数应为1～1000000000", 422);
                if (input.UsableUntil != null)
                {
                    Utc(input.UsableUntil.Value);
                    if (input.UsableUntil <= Now)
                        throw Error("InvalidReticleLife", "有效截止应晚于当前时间", 422);
                }
                var value = new Reticle
                {
                    Code = Text(input.Code, 64),
                    Name = Text(input.Name, 200),
                    Revision = Text(input.Revision, 32),
                    LayerCode = Text(input.LayerCode, 64),
                    FabId = fabId,
                    MaxUsageCount = input.MaxUsageCount,
                    UsableUntil = input.UsableUntil,
                    Status = "Available"
                };
                db.Add(value);
                return await Saved(value, "ReticleRegistered", input, true);
            }
            if (p[1] == "consumables")
            {
                var input = Body<RegisterConsumable>(r);
                const string typeCode = "SYS-EQUIPMENT-CONSUMABLE";
                var type = await db.Set<MaterialType>().SingleOrDefaultAsync(x => x.Code == typeCode);
                if (type == null)
                {
                    type = new MaterialType { Code = typeCode, Name = "设备上机耗材" };
                    db.Add(type);
                }
                if (!type.IsActive)
                    throw Error("MaterialTypeInactive", "设备耗材类型已停用");
                var value = new Material
                {
                    Code = Text(input.Code, 64),
                    Name = Text(input.Name, 200),
                    MaterialTypeId = type.Id,
                    UnitOfMeasure = Text(input.Unit, 32),
                    TrackingType = "Lot"
                };
                db.Add(value);
                return await Saved(value, "ConsumableRegistered", input, true);
            }
            if (p[1] == "consumable-lots")
            {
                var input = Body<RegisterConsumableLot>(r);
                var material = await ConsumableMaterials.SingleOrDefaultAsync(x => x.Id == input.MaterialId && x.IsActive)
                    ?? throw Error("ResourceNotFound", "启用耗材不存在", 404);
                if (input.ExpiryAt != null)
                {
                    Utc(input.ExpiryAt.Value);
                    if (input.ExpiryAt <= Now)
                        throw Error("MaterialExpired", "批次有效截止应晚于当前时间", 422);
                }
                const string warehouseCode = "SYS-EQUIPMENT-REGISTER";
                var warehouse = await db.Set<Warehouse>().SingleOrDefaultAsync(x => x.FabId == fabId
                    && x.Code == warehouseCode);
                if (warehouse == null)
                {
                    warehouse = new Warehouse
                    {
                        Code = warehouseCode,
                        Name = "设备耗材登记（无库存记账）",
                        FabId = fabId,
                        Kind = "Registration"
                    };
                    db.Add(warehouse);
                }
                if (!warehouse.IsActive || warehouse.Kind != "Registration")
                    throw Error("RegistrationWarehouseConflict", "耗材登记来源已停用或类别不符");
                var value = new MaterialLot
                {
                    LotNumber = Text(input.LotNumber, 64),
                    MaterialId = material.Id,
                    UnitOfMeasure = material.UnitOfMeasure,
                    ReceivedAt = Now,
                    ResourceExpiryAt = input.ExpiryAt,
                    WarehouseId = warehouse.Id,
                    Status = "Available",
                    QuantityReceived = 0,
                    QuantityAvailable = 0
                };
                db.Add(value);
                return await Saved(value, "ConsumableLotRegistered", input, true);
            }
        }
        if (p.Length == 3 && p[1] == "reticles" && r.Method == "PUT")
        {
            var value = await Reticles.SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                ?? throw Error("ResourceNotFound", "光罩不存在", 404);
            Match(r.Version, value.RowVersion);
            var mount = await db.Set<ReticleMount>().SingleOrDefaultAsync(x => x.ReticleId == value.Id
                && x.UnloadedAt == null);
            if (mount != null)
                await IdleResource(mount.EquipmentId);
            var input = Body<EditReticle>(r);
            if (input.Status is not ("Available" or "Blocked"))
                throw Error("InvalidReticleStatus", "光罩状态应为 Available 或 Blocked", 422);
            value.Name = Text(input.Name, 200);
            value.IsActive = input.IsActive;
            value.Status = input.Status;
            return await Saved(value, "ReticleUpdated", input);
        }
        if (p.Length == 3 && r.Method == "POST" && p[1] is "reticle-mounts" or "material-mounts")
        {
            var equipment = await Equipment(Id(p[2]));
            Match(r.Version, equipment.RowVersion);
            await IdleResource(equipment.Id);
            if (!equipment.IsActive)
                throw Error("EquipmentUnavailable", "停用设备不能装载资源");
            if (p[1] == "reticle-mounts")
            {
                var input = Body<LoadReticle>(r);
                var reticle = await Reticles.SingleOrDefaultAsync(x => x.Id == input.ReticleId)
                    ?? throw Error("ResourceNotFound", "本厂光罩不存在", 404);
                if (!reticle.IsActive || reticle.Status != "Available" || reticle.UsageCount >= reticle.MaxUsageCount
                    || reticle.UsableUntil <= Now)
                    throw Error("ReticleUnavailable", "光罩停用、禁用、寿命耗尽或过期");
                if (await db.Set<ReticleMount>().AnyAsync(x => x.UnloadedAt == null
                    && (x.EquipmentId == equipment.Id || x.ReticleId == reticle.Id)))
                    throw Error("ReticleOccupied", "设备已有光罩或该光罩正在其他设备上");
                var value = new ReticleMount { EquipmentId = equipment.Id, ReticleId = reticle.Id, LoadedAt = Now };
                db.Add(value);
                Touch(equipment);
                Touch(reticle);
                return await Saved(value, "ReticleLoaded", input, true);
            }
            else
            {
                var input = Body<MountConsumable>(r);
                Utc(input.OpenedAt);
                Utc(input.UsableUntil);
                var lot = await ConsumableLots.SingleOrDefaultAsync(x => x.Id == input.MaterialLotId)
                    ?? throw Error("ResourceNotFound", "本厂耗材批次不存在", 404);
                var invalid = await EquipmentResources.MaterialProblem(db, lot, fabId, Now);
                var expiry = await EquipmentResources.MaterialExpiry(db, lot, fabId);
                if (invalid != null || input.OpenedAt > Now || input.UsableUntil <= Now
                    || input.UsableUntil <= input.OpenedAt || expiry != null && input.UsableUntil > expiry)
                    throw Error("MaterialUnavailable", invalid ?? "开封/截止时间无效或超过批次有效期", 422);
                var materialLots = db.Set<MaterialLot>().Where(x => x.MaterialId == lot.MaterialId).Select(x => x.Id);
                if (await db.Set<MaterialMount>().AnyAsync(x => x.EquipmentId == equipment.Id && x.UnmountedAt == null
                    && materialLots.Contains(x.MaterialLotId)))
                    throw Error("MaterialAlreadyMounted", "设备已装载同一物料，请先卸下旧批次");
                var value = new MaterialMount
                {
                    EquipmentId = equipment.Id,
                    MaterialLotId = lot.Id,
                    MaterialId = lot.MaterialId,
                    OpenedAt = input.OpenedAt,
                    MountedAt = Now,
                    UsableUntil = input.UsableUntil,
                    Unit = lot.UnitOfMeasure
                };
                db.Add(value);
                Touch(equipment);
                return await Saved(value, "MaterialMounted", input, true);
            }
        }
        if (p.Length == 4 && p[3] == "unload" && r.Method == "POST")
        {
            var reason = Text(Body<ReasonRequest>(r).Reason, 500);
            if (p[1] == "reticle-mounts")
            {
                var row = await db.Set<ReticleMount>().SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                    ?? throw Error("ResourceNotFound", "装载记录不存在", 404);
                var equipment = await Equipment(row.EquipmentId);
                Match(r.Version, row.RowVersion);
                await IdleResource(equipment.Id);
                if (row.UnloadedAt != null)
                    throw Error("ReticleAlreadyUnloaded", "光罩已卸下");
                row.UnloadedAt = Now;
                Touch(equipment);
                return await Saved(row, "ReticleUnloaded", new { reason });
            }
            if (p[1] == "material-mounts")
            {
                var row = await db.Set<MaterialMount>().SingleOrDefaultAsync(x => x.Id == Id(p[2]))
                    ?? throw Error("ResourceNotFound", "上机记录不存在", 404);
                var equipment = await Equipment(row.EquipmentId);
                Match(r.Version, row.RowVersion);
                await IdleResource(equipment.Id);
                if (row.UnmountedAt != null)
                    throw Error("MaterialAlreadyUnmounted", "耗材已卸下");
                row.UnmountedAt = Now;
                Touch(equipment);
                return await Saved(row, "MaterialUnmounted", new { reason });
            }
        }
        throw Error("ResourceNotFound", "接口不存在", 404);
    }
}
