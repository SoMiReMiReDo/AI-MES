using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureEquipment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentType>(b =>
        {
            b.ToTable("equipment_types", "equipment", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<EquipmentGroup>(b =>
        {
            b.ToTable("equipment_groups", "equipment", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.FabId).HasColumnName("fab_id");
            b.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.FabId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<Equipment>(b =>
        {
            b.ToTable("equipment", "equipment", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.AreaId).HasColumnName("area_id");
            b.HasOne<Area>().WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LineId).HasColumnName("line_id");
            b.HasOne<Line>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentGroupId).HasColumnName("equipment_group_id");
            b.HasOne<EquipmentGroup>().WithMany().HasForeignKey(x => x.EquipmentGroupId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentTypeId).HasColumnName("equipment_type_id");
            b.HasOne<EquipmentType>().WithMany().HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OperationalState).HasColumnName("operational_state").HasMaxLength(32);
            b.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<Chamber>(b =>
        {
            b.ToTable("chambers", "equipment", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EquipmentId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<EquipmentEvent>(b =>
        {
            b.ToTable("equipment_events", "equipment", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MachineConnectionId).HasColumnName("machine_connection_id");
            b.HasOne<MachineConnection>().WithMany().HasForeignKey(x => x.MachineConnectionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(32);
            b.Property(x => x.StateFrom).HasColumnName("state_from").HasMaxLength(32);
            b.Property(x => x.StateTo).HasColumnName("state_to").HasMaxLength(32);
            b.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
            b.Property(x => x.EventTimestamp).HasColumnName("event_timestamp");
            b.Property(x => x.ReceivedAt).HasColumnName("received_at");
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            b.Property(x => x.IsSimulated).HasColumnName("is_simulated");
        });
        modelBuilder.Entity<EquipmentState>(b =>
        {
            b.ToTable("equipment_states", "equipment", t =>
            {
                t.HasCheckConstraint("ck_equipment_states_1", "ended_at IS NULL OR ended_at > started_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.State).HasColumnName("state").HasMaxLength(32);
            b.Property(x => x.StartedAt).HasColumnName("started_at");
            b.Property(x => x.EndedAt).HasColumnName("ended_at");
            b.Property(x => x.Source).HasColumnName("source").HasMaxLength(32);
            b.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
        });
        modelBuilder.Entity<Downtime>(b =>
        {
            b.ToTable("downtimes", "equipment", t =>
            {
                t.HasCheckConstraint("ck_downtimes_1", "ended_at IS NULL OR ended_at >= started_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.LineId).HasColumnName("line_id");
            b.HasOne<Line>().WithMany().HasForeignKey(x => x.LineId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReasonCodeId).HasColumnName("reason_code_id");
            b.HasOne<ReasonCode>().WithMany().HasForeignKey(x => x.ReasonCodeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ShiftId).HasColumnName("shift_id");
            b.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StartedAt).HasColumnName("started_at");
            b.Property(x => x.EndedAt).HasColumnName("ended_at");
            b.Property(x => x.Notes).HasColumnName("notes");
            b.Property(x => x.ReportedById).HasColumnName("reported_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReportedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.NeedsReason).HasColumnName("needs_reason");
            b.Property(x => x.ClassifiedAt).HasColumnName("classified_at");
            b.Property(x => x.ClassifiedById).HasColumnName("classified_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ClassifiedById).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Tool>(b =>
        {
            b.ToTable("tools", "equipment", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.EquipmentTypeId).HasColumnName("equipment_type_id");
            b.HasOne<EquipmentType>().WithMany().HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.NextServiceAt).HasColumnName("next_service_at");
            b.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<MaintenanceSchedule>(b =>
        {
            b.ToTable("maintenance_schedules", "equipment", t =>
            {
                t.HasCheckConstraint("ck_maintenance_schedules_1", "interval_value > 0");
                t.HasCheckConstraint("ck_maintenance_schedules_2", "num_nonnulls(tool_id, equipment_id) = 1");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.ToolId).HasColumnName("tool_id");
            b.HasOne<Tool>().WithMany().HasForeignKey(x => x.ToolId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(32);
            b.Property(x => x.AssignedToId).HasColumnName("assigned_to_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Frequency).HasColumnName("frequency").HasMaxLength(32);
            b.Property(x => x.IntervalValue).HasColumnName("interval_value");
            b.Property(x => x.LastExecutedAt).HasColumnName("last_executed_at");
            b.Property(x => x.NextDueAt).HasColumnName("next_due_at");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        });
        modelBuilder.Entity<MaintenanceEvent>(b =>
        {
            b.ToTable("maintenance_events", "equipment", t =>
            {
                t.HasCheckConstraint("ck_maintenance_events_1", "num_nonnulls(tool_id, equipment_id) = 1");
                t.HasCheckConstraint("ck_maintenance_events_2", "completed_at IS NULL OR started_at IS NULL OR completed_at >= started_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
            b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(32);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.ToolId).HasColumnName("tool_id");
            b.HasOne<Tool>().WithMany().HasForeignKey(x => x.ToolId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ScheduleId).HasColumnName("schedule_id");
            b.HasOne<MaintenanceSchedule>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.AssignedToId).HasColumnName("assigned_to_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ScheduledAt).HasColumnName("scheduled_at");
            b.Property(x => x.StartedAt).HasColumnName("started_at");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.Property(x => x.ResolutionNotes).HasColumnName("resolution_notes");
        });
        modelBuilder.Entity<EquipmentQualification>(b =>
        {
            b.ToTable("equipment_qualifications", "equipment", t =>
            {
                t.HasCheckConstraint("ck_equipment_qualifications_1", "valid_to IS NULL OR valid_to > valid_from");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OperationId).HasColumnName("operation_id");
            b.HasOne<Operation>().WithMany().HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RecipeVersionId).HasColumnName("recipe_version_id");
            b.HasOne<RecipeVersion>().WithMany().HasForeignKey(x => x.RecipeVersionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.QualificationStatus).HasColumnName("qualification_status").HasMaxLength(32);
            b.Property(x => x.ValidFrom).HasColumnName("valid_from");
            b.Property(x => x.ValidTo).HasColumnName("valid_to");
            b.Property(x => x.QualifiedById).HasColumnName("qualified_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.QualifiedById).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<WorkerQualification>(b =>
        {
            b.ToTable("worker_qualifications", "equipment", t =>
            {
                t.HasCheckConstraint("ck_worker_qualifications_1", "valid_to IS NULL OR valid_to > valid_from");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.WorkerId).HasColumnName("worker_id");
            b.HasOne<Worker>().WithMany().HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OperationId).HasColumnName("operation_id");
            b.HasOne<Operation>().WithMany().HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RecipeVersionId).HasColumnName("recipe_version_id");
            b.HasOne<RecipeVersion>().WithMany().HasForeignKey(x => x.RecipeVersionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.QualificationStatus).HasColumnName("qualification_status").HasMaxLength(32);
            b.Property(x => x.ValidFrom).HasColumnName("valid_from");
            b.Property(x => x.ValidTo).HasColumnName("valid_to");
            b.Property(x => x.QualifiedById).HasColumnName("qualified_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.QualifiedById).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Reticle>(b =>
        {
            b.ToTable("reticles", "equipment", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.Revision).HasColumnName("revision").HasMaxLength(32);
            b.Property(x => x.LayerCode).HasColumnName("layer_code").HasMaxLength(64);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.UsageCount).HasColumnName("usage_count");
            b.HasIndex(x => new { x.Code, x.Revision }).IsUnique();
        });
        modelBuilder.Entity<ReticleUsage>(b =>
        {
            b.ToTable("reticle_usages", "equipment", t =>
            {
                t.HasCheckConstraint("ck_reticle_usages_1", "unloaded_at IS NULL OR unloaded_at >= loaded_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.ReticleId).HasColumnName("reticle_id");
            b.HasOne<Reticle>().WithMany().HasForeignKey(x => x.ReticleId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            b.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LoadedAt).HasColumnName("loaded_at");
            b.Property(x => x.UnloadedAt).HasColumnName("unloaded_at");
        });
        modelBuilder.Entity<MaterialMount>(b =>
        {
            b.ToTable("material_mounts", "equipment", t =>
            {
                t.HasCheckConstraint("ck_material_mounts_1", "usage_quantity >= 0");
                t.HasCheckConstraint("ck_material_mounts_2", "unmounted_at IS NULL OR unmounted_at >= mounted_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.MaterialLotId).HasColumnName("material_lot_id");
            b.HasOne<MaterialLot>().WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OpenedAt).HasColumnName("opened_at");
            b.Property(x => x.MountedAt).HasColumnName("mounted_at");
            b.Property(x => x.UnmountedAt).HasColumnName("unmounted_at");
            b.Property(x => x.UsableUntil).HasColumnName("usable_until");
            b.Property(x => x.UsageQuantity).HasColumnName("usage_quantity").HasPrecision(20, 6);
            b.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(32);
        });
    }
}
