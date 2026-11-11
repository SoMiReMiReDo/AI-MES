using MESagent.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureIntegrity(ModelBuilder b)
    {
        b.Entity<ProductRevision>().HasAlternateKey(x => new { x.Id, x.ProductId });
        b.Entity<WorkOrder>().HasOne<ProductRevision>().WithMany()
            .HasForeignKey(x => new { x.ProductRevisionId, x.ProductId })
            .HasPrincipalKey(x => new { x.Id, x.ProductId }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RouteStep>().HasAlternateKey(x => new { x.Id, x.RouteVersionId });
        b.Entity<Lot>().HasAlternateKey(x => new { x.Id, x.RouteVersionId });
        b.Entity<Lot>().HasOne<RouteStep>().WithMany()
            .HasForeignKey(x => new { x.CurrentRouteStepId, x.RouteVersionId })
            .HasPrincipalKey(x => new { x.Id, x.RouteVersionId }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StepExecution>().HasOne<RouteStep>().WithMany()
            .HasForeignKey(x => new { x.RouteStepId, x.RouteVersionId })
            .HasPrincipalKey(x => new { x.Id, x.RouteVersionId }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StepExecution>().HasOne<Lot>().WithMany()
            .HasForeignKey(x => new { x.LotId, x.RouteVersionId })
            .HasPrincipalKey(x => new { x.Id, x.RouteVersionId }).OnDelete(DeleteBehavior.Restrict);

        b.Entity<LotMembership>().HasIndex(x => x.WaferId).IsUnique().HasFilter("valid_to IS NULL");
        b.Entity<LotMembership>().HasIndex(x => new { x.LotId, x.ValidFrom });
        b.Entity<CarrierLoad>().HasIndex(x => x.WaferId).IsUnique().HasFilter("unloaded_at IS NULL");
        b.Entity<CarrierLoad>().HasIndex(x => x.CarrierSlotId).IsUnique().HasFilter("unloaded_at IS NULL");
        b.Entity<ProcessRun>().HasIndex(x => x.EquipmentId).IsUnique().HasFilter("status = 'Running'");
        b.Entity<ProcessRun>().HasIndex(x => x.StepExecutionId).IsUnique().HasFilter("status = 'Running'");
        b.Entity<StepExecution>().HasIndex(x => x.LotId).IsUnique().HasFilter("exited_at IS NULL");
        b.Entity<EquipmentState>().HasIndex(x => x.EquipmentId).IsUnique().HasFilter("ended_at IS NULL");
        b.Entity<HoldRecord>().HasIndex(x => new { x.LotId, x.HeldAt }).HasFilter("released_at IS NULL");
        b.Entity<WarehouseStock>()
            .HasIndex(x => new { x.WarehouseId, x.MaterialId, x.ProductId, x.MaterialLotId })
            .IsUnique()
            .AreNullsDistinct(false);
        b.Entity<MaterialLot>().HasAlternateKey(x => new { x.Id, x.MaterialId });
        b.Entity<WarehouseStock>().HasOne<MaterialLot>().WithMany()
            .HasForeignKey(x => new { x.MaterialLotId, x.MaterialId })
            .HasPrincipalKey(x => new { x.Id, x.MaterialId }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<WaferDisposition>().HasIndex(x => x.WaferId).IsUnique().HasFilter("disposition_type = 'Scrap'");
        b.Entity<EquipmentEvent>().HasIndex(x => new { x.EquipmentId, x.EventTimestamp });
        b.Entity<TrackEvent>().HasIndex(x => new { x.ProcessRunId, x.OccurredAt });
        b.Entity<Measurement>().HasIndex(x => new { x.WaferId, x.MeasuredAt });
        b.Entity<AuditLog>().HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
        b.Entity<AuditLog>().HasIndex(x => x.CorrelationId);
        b.Entity<BusinessEvent>().HasIndex(x => x.CorrelationId);
        b.Entity<OutboxMessage>()
            .HasIndex(x => new { x.NextAttemptAt, x.OccurredAt })
            .HasFilter("published_at IS NULL");
        b.Entity<MachineMessage>()
            .HasIndex(x => new { x.MachineConnectionId, x.SourceMessageId })
            .IsUnique()
            .HasFilter("source_message_id IS NOT NULL");
    }
}
