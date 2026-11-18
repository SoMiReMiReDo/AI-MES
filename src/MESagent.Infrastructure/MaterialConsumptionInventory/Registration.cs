using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.MaterialConsumptionInventory;

internal sealed partial class MaterialBackend
{
    private async Task<(string Entity, Guid Id, object Data)> RegisterMaterial(RegisterWaferMaterial input)
    {
        var code = Text(input.Code, 64);
        var name = Text(input.Name, 200);
        if (await db.Set<Material>().AnyAsync(x => x.Code == code))
            throw Conflict("物料代码已登记");

        var type = await db.Set<MaterialType>().SingleOrDefaultAsync(x => x.Code == WaferOrigins.TypeCode);
        if (type == null)
        {
            type = new MaterialType { Code = WaferOrigins.TypeCode, Name = "硅片来料" };
            db.Add(type);
        }
        if (!type.IsActive)
            throw Conflict("硅片物料类别停用");

        var material = new Material
        {
            Code = code,
            Name = name,
            MaterialTypeId = type.Id,
            UnitOfMeasure = WaferOrigins.Unit,
            TrackingType = "Lot"
        };
        db.Add(material);
        return (nameof(Material), material.Id, new { material.Id, material.Code, material.Name });
    }

    private async Task<(string Entity, Guid Id, object Data)> RegisterSource(RegisterWaferMaterialSource input)
    {
        var code = Text(input.Code, 128);
        var name = Text(input.Name, 200);
        var material = await ActiveMaterials.SingleOrDefaultAsync(x => x.Id == input.MaterialId)
            ?? throw NotFound();
        if (await Sources.AnyAsync(x => x.MaterialId == material.Id && x.ExternalCode == code))
            throw Conflict("本厂该物料的来源代码已登记");

        var source = new MaterialSource
        {
            MaterialId = material.Id,
            ManualFabId = fabId,
            ExternalCode = code,
            ExternalName = name,
            ConversionFactor = 1,
            SyncEnabled = false
        };
        db.Add(source);
        return (nameof(MaterialSource), source.Id, new { source.Id, source.MaterialId, code, name });
    }

    private async Task<(string Entity, Guid Id, object Data)> RegisterLot(
        RegisterWaferMaterialLot input, DateTime time)
    {
        var lotNumber = Text(input.LotNumber, 64);
        var supplierLotNo = Text(input.SupplierLotNo, 128);
        if (input.ExpiryDate < await WaferOrigins.FabDate(db, fabId, time))
            throw Invalid("有效截止日不能早于厂区登记日");

        var source = await Sources.SingleOrDefaultAsync(x => x.Id == input.SourceId
            && ActiveMaterials.Any(m => m.Id == x.MaterialId)) ?? throw NotFound();
        if (await db.Set<MaterialLot>().AnyAsync(x => x.MaterialId == source.MaterialId && x.LotNumber == lotNumber))
            throw Conflict("该物料的来料批号已登记");

        var warehouse = await db.Set<Warehouse>().SingleOrDefaultAsync(x =>
            x.FabId == fabId && x.Code == WaferOrigins.WarehouseCode);
        if (warehouse == null)
        {
            warehouse = new Warehouse
            {
                Code = WaferOrigins.WarehouseCode,
                Name = "硅片来料来源登记",
                FabId = fabId,
                Kind = "Registration"
            };
            db.Add(warehouse);
        }
        if (warehouse.Kind != "Registration" || !warehouse.IsActive)
            throw Conflict("硅片来料登记仓停用或类别不符");

        var lot = new MaterialLot
        {
            MaterialId = source.MaterialId,
            SourceId = source.Id,
            WarehouseId = warehouse.Id,
            LotNumber = lotNumber,
            SupplierLotNo = supplierLotNo,
            ExpiryDate = input.ExpiryDate,
            ReceivedAt = time,
            Status = "Registered",
            UnitOfMeasure = WaferOrigins.Unit
        };
        db.Add(lot);
        return (nameof(MaterialLot), lot.Id, new
        {
            lot.Id,
            lot.MaterialId,
            lot.SourceId,
            lot.LotNumber,
            lot.SupplierLotNo,
            lot.ExpiryDate,
            lot.Status
        });
    }
}
