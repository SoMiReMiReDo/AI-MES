using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.MaterialConsumptionInventory;

internal sealed partial class MaterialBackend
{
    private IQueryable<Material> ActiveMaterials => WaferOrigins.Materials(db).Where(x => x.IsActive
        && x.UnitOfMeasure == WaferOrigins.Unit && x.TrackingType == "Lot"
        && db.Set<MaterialType>().Any(t => t.Id == x.MaterialTypeId && t.IsActive));

    private IQueryable<MaterialSource> Sources => db.Set<MaterialSource>().Where(x => x.ManualFabId == fabId);

    private static int Number(OrganizationIdentityAccessControlRequest r, string key, int fallback, int max)
    {
        if (!r.Query.TryGetValue(key, out var text))
            return fallback;
        if (!int.TryParse(text, out var value) || value < 1 || value > max)
            throw Invalid($"{key} 应为 1～{max}");
        return value;
    }

    private static async Task<object> Page<T>(IQueryable<T> rows, OrganizationIdentityAccessControlRequest r)
    {
        var page = Number(r, "page", 1, 1_000_000);
        var pageSize = Number(r, "pageSize", 20, 100);
        var total = await rows.CountAsync();
        var items = await rows.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new { items, total, page, pageSize };
    }

    private async Task<object> Query(OrganizationIdentityAccessControlRequest r, string[] path)
    {
        var search = r.Query.GetValueOrDefault("search", "").Trim();
        if (search.Length > 200)
            throw Invalid("检索文本最多 200 字符");

        if (path[1] == "wafer-materials")
        {
            var materials = ActiveMaterials.Where(x => x.Code.Contains(search) || x.Name.Contains(search));
            return await Page(materials.OrderBy(x => x.Code).ThenBy(x => x.Id)
                .Select(x => new { x.Id, x.Code, x.Name }), r);
        }
        if (path[1] == "wafer-sources")
        {
            var sources = from source in Sources
                          join material in ActiveMaterials on source.MaterialId equals material.Id
                          where source.ExternalCode.Contains(search)
                              || source.ExternalName != null && source.ExternalName.Contains(search)
                              || material.Code.Contains(search)
                          select new
                          {
                              source.Id,
                              source.MaterialId,
                              code = source.ExternalCode,
                              name = source.ExternalName,
                              materialCode = material.Code
                          };
            if (r.Query.TryGetValue("materialId", out var materialId))
            {
                var id = Id(materialId);
                sources = sources.Where(x => x.MaterialId == id);
            }
            return await Page(sources.OrderBy(x => x.code).ThenBy(x => x.Id), r);
        }

        var lots = WaferOrigins.Lots(db, fabId);
        var now = Now;
        var today = await WaferOrigins.FabDate(db, fabId, now);
        var selectable = WaferOrigins.Selectable(db, lots, today, now);
        if (path.Length == 2)
        {
            var matching = (path[1] == "wafer-origin-options" ? selectable : lots).Where(x =>
                x.Lot.LotNumber.Contains(search)
                || x.Lot.SupplierLotNo != null && x.Lot.SupplierLotNo.Contains(search)
                || x.Material.Code.Contains(search) || x.Source.ExternalCode.Contains(search))
                .OrderBy(x => x.Lot.LotNumber).ThenBy(x => x.Lot.Id);
            if (path[1] == "wafer-origin-options")
                return await Page(matching.Select(x => new
                {
                    x.Lot.Id,
                    x.Lot.LotNumber,
                    x.Lot.SupplierLotNo,
                    materialCode = x.Material.Code
                }), r);

            return await Page(matching.Select(x => new
            {
                x.Lot.Id,
                x.Lot.LotNumber,
                x.Lot.SupplierLotNo,
                x.Lot.ReceivedAt,
                x.Lot.ExpiryDate,
                materialCode = x.Material.Code,
                sourceCode = x.Source.ExternalCode
            }), r);
        }

        var lotId = Id(path[2]);
        if (path.Length == 4)
        {
            require("production.wafers.read");
            if (!await lots.AnyAsync(x => x.Lot.Id == lotId))
                throw NotFound();
            return await Wafers(lotId, r);
        }

        var entry = await lots.SingleOrDefaultAsync(x => x.Lot.Id == lotId) ?? throw NotFound();
        return new
        {
            entry.Lot.Id,
            entry.Lot.LotNumber,
            entry.Lot.SupplierLotNo,
            entry.Lot.ReceivedAt,
            entry.Lot.ExpiryDate,
            entry.Lot.Status,
            selectable = await selectable.AnyAsync(x => x.Lot.Id == lotId),
            materialCode = entry.Material.Code,
            materialName = entry.Material.Name,
            sourceCode = entry.Source.ExternalCode,
            sourceName = entry.Source.ExternalName
        };
    }

    private Task<object> Wafers(Guid materialLotId, OrganizationIdentityAccessControlRequest r)
    {
        var visibleLots = db.Set<Lot>().Where(x => x.FabId == fabId
            && x.WorkOrderId != null && x.SnapshotVersion != null);
        var currentLots = from membership in db.Set<LotMembership>()
                          join lot in visibleLots on membership.LotId equals lot.Id
                          where membership.ValidTo == null
                          select new { membership.WaferId, lot.Id, lot.LotNumber };
        var showLot = can("production.lots.read");
        var rows = db.Set<Wafer>().Where(x => x.FabId == fabId && x.WorkOrderId != null
                && x.OriginMaterialLotId == materialLotId)
            .OrderBy(x => x.WaferCode).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.WaferCode,
                x.SupplierWaferCode,
                x.LifecycleStatus,
                currentLotId = showLot
                    ? currentLots.Where(l => l.WaferId == x.Id).Select(l => (Guid?)l.Id).FirstOrDefault() : null,
                currentLotNumber = showLot
                    ? currentLots.Where(l => l.WaferId == x.Id).Select(l => l.LotNumber).FirstOrDefault() : null
            });
        return Page(rows, r);
    }
}
