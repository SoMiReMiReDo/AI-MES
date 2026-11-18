using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureEquipmentResources(ModelBuilder b)
    {
        b.Entity<MaterialLot>().Property(x => x.ResourceExpiryAt).HasColumnName("resource_expiry_at");
        b.Entity<Equipment>().Property(x => x.CommunicationState).HasColumnName("communication_state")
            .HasMaxLength(32).HasDefaultValue("Unknown");
        b.Entity<EquipmentEvent>(e =>
        {
            e.Property(x => x.Source).HasColumnName("source").HasMaxLength(64).HasDefaultValue("Manual");
            e.Property(x => x.SourceEventId).HasColumnName("source_event_id").HasMaxLength(128);
            e.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(32)
                .HasDefaultValue("Applied");
            e.Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
            e.HasIndex(x => new { x.EquipmentId, x.Source, x.SourceEventId }).IsUnique()
                .HasFilter("source_event_id IS NOT NULL AND machine_connection_id IS NULL");
            e.HasIndex(x => new { x.MachineConnectionId, x.Source, x.SourceEventId }).IsUnique()
                .HasFilter("source_event_id IS NOT NULL AND machine_connection_id IS NOT NULL");
        });
        b.Entity<Downtime>(e =>
        {
            e.Property(x => x.EquipmentStateId).HasColumnName("equipment_state_id");
            e.HasOne<EquipmentState>().WithMany().HasForeignKey(x => x.EquipmentStateId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.EquipmentStateId).IsUnique().HasFilter("equipment_state_id IS NOT NULL");
        });
        b.Entity<MaterialMount>(e =>
        {
            e.Property(x => x.MaterialId).HasColumnName("material_id");
            e.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.EquipmentId, x.MaterialId }).IsUnique()
                .HasFilter("unmounted_at IS NULL AND material_id IS NOT NULL");
        });
        b.Entity<MaintenanceEvent>().HasIndex(x => x.ScheduleId).IsUnique()
            .HasFilter("schedule_id IS NOT NULL AND status NOT IN ('Released','Cancelled')");
        b.Entity<Tool>(e =>
        {
            e.Property(x => x.FabId).HasColumnName("fab_id");
            e.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            e.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<MaintenanceEvent>(e =>
        {
            e.Property(x => x.ReleasedAt).HasColumnName("released_at");
            e.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            e.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Reticle>(e =>
        {
            e.Property(x => x.FabId).HasColumnName("fab_id");
            e.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.MaxUsageCount).HasColumnName("max_usage_count").HasDefaultValue(1000L);
            e.Property(x => x.UsableUntil).HasColumnName("usable_until");
            e.ToTable("reticles", "equipment", t =>
                t.HasCheckConstraint("ck_reticle_life", "usage_count >= 0 AND max_usage_count > 0"));
        });
        Resource<EquipmentOperationPolicy>(b, "operation_policies");
        b.Entity<EquipmentOperationPolicy>(e =>
        {
            e.Property(x => x.OperationId).HasColumnName("operation_id");
            e.HasOne<Operation>().WithMany().HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.OperationId).IsUnique();
            e.Property(x => x.RequireEquipmentQualification).HasColumnName("require_equipment_qualification");
            e.Property(x => x.RequireWorkerQualification).HasColumnName("require_worker_qualification");
            e.Property(x => x.RequiredReticleLayer).HasColumnName("required_reticle_layer").HasMaxLength(64);
        });
        Resource<EquipmentMaterialRequirement>(b, "material_requirements");
        b.Entity<EquipmentMaterialRequirement>(e =>
        {
            e.Property(x => x.OperationId).HasColumnName("operation_id");
            e.Property(x => x.MaterialId).HasColumnName("material_id");
            e.HasOne<Operation>().WithMany().HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.OperationId, x.MaterialId }).IsUnique();
        });
        Resource<ReticleMount>(b, "reticle_mounts");
        b.Entity<ReticleMount>(e =>
        {
            e.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            e.Property(x => x.ReticleId).HasColumnName("reticle_id");
            e.Property(x => x.LoadedAt).HasColumnName("loaded_at");
            e.Property(x => x.UnloadedAt).HasColumnName("unloaded_at");
            e.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Reticle>().WithMany().HasForeignKey(x => x.ReticleId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.ReticleId).IsUnique().HasFilter("unloaded_at IS NULL");
            e.HasIndex(x => x.EquipmentId).IsUnique().HasFilter("unloaded_at IS NULL");
            e.ToTable("reticle_mounts", "equipment", t =>
                t.HasCheckConstraint("ck_reticle_mount_time", "unloaded_at IS NULL OR unloaded_at >= loaded_at"));
        });
        b.Entity<ReticleUsage>(e =>
        {
            e.Property(x => x.ReticleMountId).HasColumnName("reticle_mount_id");
            e.HasOne<ReticleMount>().WithMany().HasForeignKey(x => x.ReticleMountId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.ReticleId, x.ProcessRunId }).IsUnique();
        });
        Resource<MaterialRunUsage>(b, "material_run_usages");
        b.Entity<MaterialRunUsage>(e =>
        {
            e.Property(x => x.MaterialMountId).HasColumnName("material_mount_id");
            e.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            e.HasOne<MaterialMount>().WithMany().HasForeignKey(x => x.MaterialMountId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.MaterialMountId, x.ProcessRunId }).IsUnique();
        });
    }

    private static void Resource<T>(ModelBuilder b, string table) where T : class
    {
        b.Entity<T>(e =>
        {
            e.ToTable(table, "equipment");
            e.HasKey("Id");
            e.Property<Guid>("Id").HasColumnName("id").HasDefaultValueSql("uuidv7()");
            e.Property<DateTime>("CreatedAt").HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property<DateTime>("UpdatedAt").HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property<uint>("RowVersion").HasColumnName("xmin").IsRowVersion();
        });
    }
}
