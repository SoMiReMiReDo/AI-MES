using MESagent.Application;
using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.TraceabilityImpactAnalysis;

internal sealed partial class TraceabilityBackend
{
    // Every branch projects the same columns for database-side filtering and pagination across event types.
    private sealed class Fact
    {
        public Guid Id { get; init; }
        public string Type { get; init; } = "";
        public DateTime OccurredAt { get; init; }
        public DateTime? EndedAt { get; init; }
        public Guid? LotId { get; init; }
        public Guid? WaferId { get; init; }
        public Guid? ReferenceId { get; init; }
        public Guid? ActorId { get; init; }
        public string? Summary { get; init; }
        public string? Result { get; init; }
    }

    private IQueryable<Lot> SubjectLots(TraceObject subject) => subject.Kind switch
    {
        "work-order" => Lots.Where(x => x.WorkOrderId == subject.Id),
        "lot" => Lots.Where(x => x.Id == subject.Id),
        _ => Lots
    };

    private IQueryable<LotMembership> Memberships => db.Set<LotMembership>().Where(x =>
        Lots.Any(l => l.Id == x.LotId) && Wafers.Any(w => w.Id == x.WaferId));

    private IQueryable<Fact> Events(TraceObject subject, string[] groups) => groups.Select(group => group switch
    {
        "orders" => OrderEvents(subject),
        "lots" => LotEvents(subject),
        "wafers" => WaferEvents(subject),
        "genealogy" => GenealogyEvents(subject),
        "carriers" => CarrierEvents(subject),
        "execution" => ExecutionEvents(subject),
        "quality" => QualityEvents(subject),
        "equipment" => EquipmentEvents(subject),
        _ => throw Invalid("履历类别无效")
    }).Aggregate((events, group) => events.Concat(group));

    private IQueryable<Fact> OrderEvents(TraceObject subject)
    {
        var orders = Orders.Where(x => x.Id == subject.Id);
        var created = orders.Select(x => new Fact
        {
            Id = x.Id, Type = "order-created", OccurredAt = x.CreatedAt, EndedAt = null,
            LotId = null, WaferId = null, ReferenceId = null, ActorId = null, Summary = x.OrderNo, Result = null
        });
        var released = db.Set<WorkOrderSnapshot>().Where(x => orders.Any(o => o.Id == x.WorkOrderId))
            .Select(x => new Fact
            {
                Id = x.Id, Type = "order-released", OccurredAt = x.EffectiveFrom, EndedAt = null,
                LotId = null, WaferId = null, ReferenceId = null, ActorId = x.CreatedById,
                Summary = "下达快照 " + x.Version.ToString(), Result = null
            });
        var completed = orders.Where(x => x.CompletedAt != null).Select(x => new Fact
        {
            Id = x.Id, Type = "order-completed", OccurredAt = x.CompletedAt!.Value, EndedAt = null,
            LotId = null, WaferId = null, ReferenceId = null, ActorId = null, Summary = x.OrderNo, Result = null
        });
        return created.Concat(released).Concat(completed);
    }

    private IQueryable<Fact> LotEvents(TraceObject subject)
    {
        var lots = SubjectLots(subject);
        var created = lots.Select(x => new Fact
        {
            Id = x.Id, Type = "lot-created", OccurredAt = x.CreatedAt, EndedAt = null,
            LotId = x.Id, WaferId = null, ReferenceId = null, ActorId = null, Summary = x.LotNumber, Result = null
        });
        var closed = lots.Where(x => x.ClosedAt != null).Select(x => new Fact
        {
            Id = x.Id, Type = "lot-closed", OccurredAt = x.ClosedAt!.Value, EndedAt = null,
            LotId = x.Id, WaferId = null, ReferenceId = null, ActorId = null, Summary = x.CloseReason, Result = x.Status
        });
        return created.Concat(closed);
    }

    private IQueryable<Fact> WaferEvents(TraceObject subject)
    {
        var lots = SubjectLots(subject);
        var waferId = subject.Kind == "wafer" ? subject.Id : (Guid?)null;
        var membership = Memberships.Where(x => (waferId == null || x.WaferId == waferId)
                && lots.Any(l => l.Id == x.LotId))
            .Select(x => new Fact
            {
                Id = x.Id, Type = "membership", OccurredAt = x.ValidFrom, EndedAt = x.ValidTo,
                LotId = x.LotId, WaferId = x.WaferId, ReferenceId = null, ActorId = null, Summary = null, Result = null
            });
        var released = db.Set<WaferRelease>().Where(x => (waferId == null || x.WaferId == waferId)
                && Wafers.Any(w => w.Id == x.WaferId) && lots.Any(l => l.Id == x.InitialLotId))
            .Select(x => new Fact
            {
                Id = x.Id, Type = "wafer-released", OccurredAt = x.ReleasedAt, EndedAt = null,
                LotId = x.InitialLotId, WaferId = x.WaferId, ReferenceId = null, ActorId = x.ReleasedById,
                Summary = null, Result = null
            });
        var completed = db.Set<WaferCompletion>().Where(x => (waferId == null || x.WaferId == waferId)
                && Wafers.Any(w => w.Id == x.WaferId) && lots.Any(l => l.Id == x.LotIdAtCompletion))
            .Select(x => new Fact
            {
                Id = x.Id, Type = "wafer-completed", OccurredAt = x.CompletedAt, EndedAt = null,
                LotId = x.LotIdAtCompletion, WaferId = x.WaferId, ReferenceId = null, ActorId = x.CompletedById,
                Summary = null, Result = null
            });
        return membership.Concat(released).Concat(completed);
    }

    private IQueryable<Fact> GenealogyEvents(TraceObject subject)
    {
        var subjectLots = SubjectLots(subject);
        var waferId = subject.Kind == "wafer" ? subject.Id : (Guid?)null;
        return from detail in db.Set<GenealogyDetail>()
               join evt in db.Set<GenealogyEvent>() on detail.EventId equals evt.Id
               where (evt.EventType == "Split" || evt.EventType == "Merge")
                   && Wafers.Any(w => w.Id == detail.WaferId)
                   && Lots.Any(l => l.Id == detail.FromLotId) && Lots.Any(l => l.Id == detail.ToLotId)
                   && (waferId == null || detail.WaferId == waferId)
                   && subjectLots.Any(l => l.Id == detail.FromLotId || l.Id == detail.ToLotId)
               select new Fact
               {
                   Id = detail.Id, Type = "genealogy", OccurredAt = evt.OccurredAt, EndedAt = null,
                   LotId = detail.ToLotId, WaferId = detail.WaferId, ReferenceId = detail.FromLotId,
                   ActorId = evt.PerformedById, Summary = evt.Reason, Result = evt.EventType
               };
    }

    private IQueryable<CarrierSlot> Slots => db.Set<CarrierSlot>().Where(x => Carriers.Any(c => c.Id == x.CarrierId));

    private IQueryable<CarrierLoad> Loads => db.Set<CarrierLoad>().Where(x =>
        Slots.Any(s => s.Id == x.CarrierSlotId) && Wafers.Any(w => w.Id == x.WaferId));

    private IQueryable<Fact> CarrierEvents(TraceObject subject)
    {
        var loads = from load in Loads
                    join slot in Slots on load.CarrierSlotId equals slot.Id
                    where (subject.Kind != "wafer" || load.WaferId == subject.Id)
                        && (subject.Kind != "carrier" || slot.CarrierId == subject.Id)
                    select new { load, slot };
        var members = from member in Memberships
                      join lot in SubjectLots(subject) on member.LotId equals lot.Id
                      select member;
        var byLot = subject.Kind is "lot" or "work-order";
        var loaded = from row in loads
                     from member in members.Where(m => m.WaferId == row.load.WaferId
                         && m.ValidFrom <= row.load.LoadedAt && (m.ValidTo == null || row.load.LoadedAt < m.ValidTo))
                         .DefaultIfEmpty()
                     where !byLot || member != null
                     select new Fact
                     {
                         Id = row.load.Id, Type = "carrier-loaded", OccurredAt = row.load.LoadedAt, EndedAt = null,
                         LotId = (Guid?)member!.LotId, WaferId = row.load.WaferId, ReferenceId = row.slot.CarrierId,
                         ActorId = null, Summary = "槽位 " + row.slot.SlotNo.ToString(), Result = null
                     };
        // A closing disposition may unload the wafer and close its previous membership at the same instant.
        var unloaded = from row in loads
                       where row.load.UnloadedAt != null
                       from member in members.Where(m => m.WaferId == row.load.WaferId
                           && m.ValidFrom < row.load.UnloadedAt && (m.ValidTo == null || row.load.UnloadedAt <= m.ValidTo))
                           .DefaultIfEmpty()
                       where !byLot || member != null
                       select new Fact
                       {
                           Id = row.load.Id, Type = "carrier-unloaded", OccurredAt = row.load.UnloadedAt!.Value,
                           EndedAt = null, LotId = (Guid?)member!.LotId, WaferId = row.load.WaferId,
                           ReferenceId = row.slot.CarrierId, ActorId = null,
                           Summary = "槽位 " + row.slot.SlotNo.ToString(), Result = null
                       };
        if (subject.Kind != "carrier")
            return loaded.Concat(unloaded);

        var moves = from move in db.Set<CarrierMovement>()
                    join destination in Locations on move.ToLocationId equals destination.Id
                    where move.CarrierId == subject.Id
                        && (move.FromLocationId == null || Locations.Any(l => l.Id == move.FromLocationId))
                    select new Fact
                    {
                        Id = move.Id, Type = "carrier-moved", OccurredAt = move.MovedAt, EndedAt = null,
                        LotId = null, WaferId = null, ReferenceId = subject.Id,
                        ActorId = move.PerformedById,
                        Summary = destination.Name, Result = null
                    };
        return loaded.Concat(unloaded).Concat(moves);
    }
}
