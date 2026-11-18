using MESagent.Application;
using MESagent.Domain;
using MESagent.Domain.Entities;
using MESagent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.EquipmentMaintenanceResources;

public static partial class EquipmentResources
{
    public static IQueryable<Equipment> EquipmentScope(MesDbContext db, Guid fabId) =>
        from e in db.Set<Equipment>()
        join area in db.Set<Area>() on e.AreaId equals area.Id
        join equipmentGroup in db.Set<EquipmentGroup>() on e.EquipmentGroupId equals equipmentGroup.Id
        where area.FabId == fabId && equipmentGroup.FabId == fabId
        select e;

    public static Task<Equipment?> ScopedEquipment(MesDbContext db, Guid fabId, Guid id) =>
        EquipmentScope(db, fabId).SingleOrDefaultAsync(x => x.Id == id);

    public static async Task RefreshState(MesDbContext db, Equipment equipment, DateTime now, Guid actorId,
        string source, Guid? reasonCodeId = null, Guid? shiftId = null, string? notes = null)
    {
        var trackedRuns = db.ChangeTracker.Entries<ProcessRun>().Select(x => x.Entity.Id).ToArray();
        var running = db.ChangeTracker.Entries<ProcessRun>().Any(x => x.Entity.EquipmentId == equipment.Id
            && x.Entity.Status == "Running") || await db.Set<ProcessRun>().AnyAsync(x =>
                x.EquipmentId == equipment.Id && x.Status == "Running"
                && !trackedRuns.Contains(x.Id));
        var effective = equipment.OperationalState == "Idle" && running ? "Running" : equipment.OperationalState;
        var current = await db.Set<EquipmentState>().SingleOrDefaultAsync(x =>
            x.EquipmentId == equipment.Id && x.EndedAt == null);
        if (current?.State == effective)
            return;
        var time = LotRules.NextEventTime(now, current == null ? [] : [current.StartedAt]);
        if (current != null)
        {
            current.EndedAt = time;
            await db.SaveChangesAsync();
            foreach (var downtime in await db.Set<Downtime>().Where(x => x.EquipmentStateId == current.Id
                && x.EndedAt == null).ToListAsync())
                downtime.EndedAt = time;
            await db.SaveChangesAsync();
        }
        var state = new EquipmentState
        {
            EquipmentId = equipment.Id,
            State = effective,
            StartedAt = time,
            Source = source
        };
        db.Add(state);
        if (effective == "Down")
            db.Add(new Downtime
            {
                EquipmentId = equipment.Id,
                EquipmentStateId = state.Id,
                LineId = equipment.LineId,
                StartedAt = time,
                ReasonCodeId = reasonCodeId,
                ShiftId = shiftId,
                Notes = notes,
                ReportedById = actorId,
                NeedsReason = reasonCodeId == null,
                ClassifiedAt = reasonCodeId == null ? null : time,
                ClassifiedById = reasonCodeId == null ? null : actorId
            });
    }
}
