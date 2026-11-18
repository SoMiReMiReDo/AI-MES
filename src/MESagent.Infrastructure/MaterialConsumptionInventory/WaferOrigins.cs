using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.MaterialConsumptionInventory;

internal static class WaferOrigins
{
    public const string TypeCode = "SYS-WAFER-INPUT";
    public const string WarehouseCode = "SYS-WAFER-ORIGIN-REGISTER";
    public const string Unit = "PCS";

    internal sealed class Entry
    {
        public required MaterialLot Lot { get; init; }
        public required Material Material { get; init; }
        public required MaterialSource Source { get; init; }
        public required MaterialType Type { get; init; }
        public required Warehouse Warehouse { get; init; }
    }

    public static IQueryable<Material> Materials(MesDbContext db) =>
        from material in db.Set<Material>()
        join type in db.Set<MaterialType>() on material.MaterialTypeId equals type.Id
        where type.Code == TypeCode
        select material;

    public static IQueryable<Entry> Lots(MesDbContext db, Guid fabId) =>
        from lot in db.Set<MaterialLot>()
        join material in db.Set<Material>() on lot.MaterialId equals material.Id
        join type in db.Set<MaterialType>() on material.MaterialTypeId equals type.Id
        join source in db.Set<MaterialSource>() on lot.SourceId equals source.Id
        join warehouse in db.Set<Warehouse>() on lot.WarehouseId equals warehouse.Id
        where type.Code == TypeCode
            && source.MaterialId == material.Id && source.ManualFabId == fabId
            && warehouse.FabId == fabId && warehouse.Code == WarehouseCode && warehouse.Kind == "Registration"
        select new Entry { Lot = lot, Material = material, Source = source, Type = type, Warehouse = warehouse };

    public static async Task<DateOnly> FabDate(MesDbContext db, Guid fabId, DateTime now)
    {
        var zone = await db.Set<Fab>().Where(x => x.Id == fabId).Select(x => x.Timezone).SingleAsync();
        var local = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.FindSystemTimeZoneById(zone));
        return DateOnly.FromDateTime(local);
    }

    public static IQueryable<Entry> Selectable(
        MesDbContext db,
        IQueryable<Entry> lots,
        DateOnly today,
        DateTime now) =>
        lots.Where(x => x.Type.IsActive && x.Material.IsActive && x.Warehouse.IsActive
            && x.Lot.Status == "Registered" && x.Material.UnitOfMeasure == Unit
            && x.Lot.UnitOfMeasure == Unit && x.Material.TrackingType == "Lot"
            && (x.Lot.ExpiryDate == null || x.Lot.ExpiryDate >= today)
            && (x.Lot.ResourceExpiryAt == null || x.Lot.ResourceExpiryAt > now)
            && (x.Lot.HeldAt == null || x.Lot.ReleasedAt != null && x.Lot.ReleasedAt >= x.Lot.HeldAt)
            && (x.Lot.HoldReason == null || x.Lot.HoldReason == "")
            && !db.Set<MaterialHold>().Any(h => h.MaterialLotId == x.Lot.Id && h.ReleasedAt == null));
}
