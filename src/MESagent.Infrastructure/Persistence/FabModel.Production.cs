using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureProduction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkOrder>(b =>
        {
            b.ToTable("work_orders", "production", t =>
            {
                t.HasCheckConstraint("ck_work_orders_1", "planned_qty >= 0 AND produced_qty >= 0");
                t.HasCheckConstraint("ck_work_orders_2", "planned_end_at IS NULL OR planned_start_at IS NULL OR planned_end_at >= planned_start_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.OrderNo).HasColumnName("order_no").HasMaxLength(64);
            b.Property(x => x.FabId).HasColumnName("fab_id");
            b.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProductId).HasColumnName("product_id");
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProductRevisionId).HasColumnName("product_revision_id");
            b.HasOne<ProductRevision>()
                .WithMany()
                .HasForeignKey(x => x.ProductRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.PlannedQty).HasColumnName("planned_qty");
            b.Property(x => x.ProducedQty).HasColumnName("produced_qty");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.Priority).HasColumnName("priority");
            b.Property(x => x.DueDate).HasColumnName("due_date");
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.PlannedStartAt).HasColumnName("planned_start_at");
            b.Property(x => x.PlannedEndAt).HasColumnName("planned_end_at");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.Property(x => x.CustomerId).HasColumnName("customer_id");
            b.HasOne<Company>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CustomerOrderNo).HasColumnName("customer_order_no").HasMaxLength(128);
            b.Property(x => x.ProcessSnapshot).HasColumnName("process_snapshot").HasColumnType("jsonb");
            b.Property(x => x.SnapshotVersion).HasColumnName("snapshot_version");
            b.HasIndex(x => x.OrderNo).IsUnique();
        });

        modelBuilder.Entity<WorkOrderSnapshot>(b =>
        {
            b.ToTable("work_order_snapshots", "production", t =>
            {
                t.HasCheckConstraint("ck_work_order_snapshots_1", "version > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
            b.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).HasColumnName("version");
            b.Property(x => x.Snapshot).HasColumnName("snapshot").HasColumnType("jsonb");
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            b.Property(x => x.EffectiveFromLotId).HasColumnName("effective_from_lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.EffectiveFromLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ChangeRequestId).HasColumnName("change_request_id");
            b.HasOne<WorkOrderChangeRequest>()
                .WithMany()
                .HasForeignKey(x => x.ChangeRequestId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CreatedById).HasColumnName("created_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.WorkOrderId, x.Version }).IsUnique();
        });

        modelBuilder.Entity<WorkOrderChangeRequest>(b =>
        {
            b.ToTable("work_order_change_requests", "production", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
            b.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Reason).HasColumnName("reason");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.Proposed).HasColumnName("proposed").HasColumnType("jsonb");
            b.Property(x => x.PreviousValues).HasColumnName("previous_values").HasColumnType("jsonb");
            b.Property(x => x.Impact).HasColumnName("impact").HasColumnType("jsonb");
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            b.Property(x => x.EffectiveFromLotId).HasColumnName("effective_from_lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.EffectiveFromLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RequestedById).HasColumnName("requested_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.SubmittedAt).HasColumnName("submitted_at");
            b.Property(x => x.ApprovedById).HasColumnName("approved_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ApprovedAt).HasColumnName("approved_at");
            b.Property(x => x.RejectedById).HasColumnName("rejected_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.RejectedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RejectedAt).HasColumnName("rejected_at");
            b.Property(x => x.RejectionReason).HasColumnName("rejection_reason");
            b.Property(x => x.AppliedById).HasColumnName("applied_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.AppliedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.AppliedAt).HasColumnName("applied_at");
            b.Property(x => x.ResultingSnapshotVersion).HasColumnName("resulting_snapshot_version");
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Lot>(b =>
        {
            b.ToTable("lots", "production", t =>
            {
                t.HasCheckConstraint("ck_lots_1", "target_qty >= 0 AND produced_qty >= 0");
                t.HasCheckConstraint("ck_lots_2", "completed_at IS NULL OR started_at IS NULL OR completed_at >= started_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.FabId).HasColumnName("fab_id");
            b.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
            b.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotNumber).HasColumnName("lot_number").HasMaxLength(64);
            b.Property(x => x.LegacyBatchNumber).HasColumnName("legacy_batch_number").HasMaxLength(64);
            b.Property(x => x.LotAssignedAt).HasColumnName("lot_assigned_at");
            b.Property(x => x.ProductRevisionId).HasColumnName("product_revision_id");
            b.HasOne<ProductRevision>()
                .WithMany()
                .HasForeignKey(x => x.ProductRevisionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RouteVersionId).HasColumnName("route_version_id");
            b.HasOne<RouteVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CurrentRouteStepId).HasColumnName("current_route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.CurrentRouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotType).HasColumnName("lot_type").HasMaxLength(32);
            b.Property(x => x.Priority).HasColumnName("priority");
            b.Property(x => x.TargetQty).HasColumnName("target_qty");
            b.Property(x => x.ProducedQty).HasColumnName("produced_qty");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.StartedAt).HasColumnName("started_at");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.Property(x => x.ReleasedToRouteAt).HasColumnName("released_to_route_at");
            b.Property(x => x.ReleasedToRouteById).HasColumnName("released_to_route_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedToRouteById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Notes).HasColumnName("notes");
            b.Property(x => x.SnapshotVersion).HasColumnName("snapshot_version");
            b.HasIndex(x => new { x.FabId, x.LotNumber }).IsUnique();
        });

        modelBuilder.Entity<Wafer>(b =>
        {
            b.ToTable("wafers", "production", t =>
            {
                t.HasCheckConstraint("ck_wafers_1", "wafer_size_mm > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.WaferCode).HasColumnName("wafer_code").HasMaxLength(64);
            b.Property(x => x.OriginMaterialLotId).HasColumnName("origin_material_lot_id");
            b.HasOne<MaterialLot>()
                .WithMany()
                .HasForeignKey(x => x.OriginMaterialLotId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.SupplierWaferCode).HasColumnName("supplier_wafer_code").HasMaxLength(128);
            b.Property(x => x.WaferSizeMm).HasColumnName("wafer_size_mm").HasPrecision(20, 6);
            b.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(32);
            b.Property(x => x.ProducedAt).HasColumnName("produced_at");
            b.HasIndex(x => x.WaferCode).IsUnique();
        });

        modelBuilder.Entity<GenealogyEvent>(b =>
        {
            b.ToTable("genealogy_events", "production", t =>
            {
                t.HasCheckConstraint("ck_genealogy_events_1", "event_type IN ('Split', 'Merge', 'Transfer', 'Create')");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(32);
            b.Property(x => x.Reason).HasColumnName("reason");
            b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            b.Property(x => x.PerformedById).HasColumnName("performed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.PerformedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            b.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<GenealogyDetail>(b =>
        {
            b.ToTable("genealogy_details", "production", t =>
            {
                t.HasCheckConstraint("ck_genealogy_details_1", "from_lot_id IS NULL OR from_lot_id <> to_lot_id");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EventId).HasColumnName("event_id");
            b.HasOne<GenealogyEvent>().WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WaferId).HasColumnName("wafer_id");
            b.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.FromLotId).HasColumnName("from_lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.FromLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ToLotId).HasColumnName("to_lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.ToLotId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.EventId, x.WaferId }).IsUnique();
        });

        modelBuilder.Entity<LotMembership>(b =>
        {
            b.ToTable("lot_memberships", "production", t =>
            {
                t.HasCheckConstraint("ck_lot_memberships_1", "valid_to IS NULL OR valid_to > valid_from");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.WaferId).HasColumnName("wafer_id");
            b.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ValidFrom).HasColumnName("valid_from");
            b.Property(x => x.ValidTo).HasColumnName("valid_to");
            b.Property(x => x.ChangeEventId).HasColumnName("change_event_id");
            b.HasOne<GenealogyEvent>().WithMany().HasForeignKey(x => x.ChangeEventId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Carrier>(b =>
        {
            b.ToTable("carriers", "production", t =>
            {
                t.HasCheckConstraint("ck_carriers_1", "slot_capacity > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.FabId).HasColumnName("fab_id");
            b.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CarrierCode).HasColumnName("carrier_code").HasMaxLength(64);
            b.Property(x => x.CarrierType).HasColumnName("carrier_type").HasMaxLength(32);
            b.Property(x => x.SlotCapacity).HasColumnName("slot_capacity");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.CurrentLocationId).HasColumnName("current_location_id");
            b.HasOne<Location>().WithMany().HasForeignKey(x => x.CurrentLocationId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.CarrierCode).IsUnique();
        });

        modelBuilder.Entity<CarrierSlot>(b =>
        {
            b.ToTable("carrier_slots", "production", t =>
            {
                t.HasCheckConstraint("ck_carrier_slots_1", "slot_no > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.CarrierId).HasColumnName("carrier_id");
            b.HasOne<Carrier>().WithMany().HasForeignKey(x => x.CarrierId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.SlotNo).HasColumnName("slot_no");
            b.HasIndex(x => new { x.CarrierId, x.SlotNo }).IsUnique();
        });

        modelBuilder.Entity<CarrierLoad>(b =>
        {
            b.ToTable("carrier_loads", "production", t =>
            {
                t.HasCheckConstraint("ck_carrier_loads_1", "unloaded_at IS NULL OR unloaded_at > loaded_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.CarrierSlotId).HasColumnName("carrier_slot_id");
            b.HasOne<CarrierSlot>().WithMany().HasForeignKey(x => x.CarrierSlotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WaferId).HasColumnName("wafer_id");
            b.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LoadedAt).HasColumnName("loaded_at");
            b.Property(x => x.UnloadedAt).HasColumnName("unloaded_at");
        });

        modelBuilder.Entity<CarrierMovement>(b =>
        {
            b.ToTable("carrier_movements", "production", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.CarrierId).HasColumnName("carrier_id");
            b.HasOne<Carrier>().WithMany().HasForeignKey(x => x.CarrierId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.FromLocationId).HasColumnName("from_location_id");
            b.HasOne<Location>().WithMany().HasForeignKey(x => x.FromLocationId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ToLocationId).HasColumnName("to_location_id");
            b.HasOne<Location>().WithMany().HasForeignKey(x => x.ToLocationId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MovedAt).HasColumnName("moved_at");
            b.Property(x => x.PerformedById).HasColumnName("performed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.PerformedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Notes).HasColumnName("notes");
        });

        modelBuilder.Entity<StepExecution>(b =>
        {
            b.ToTable("step_executions", "production", t =>
            {
                t.HasCheckConstraint("ck_step_executions_1", "visit_no > 0 AND passed_qty >= 0 AND scrap_qty >= 0");
                t.HasCheckConstraint("ck_step_executions_2", "exited_at IS NULL OR exited_at >= entered_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RouteVersionId).HasColumnName("route_version_id");
            b.HasOne<RouteVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RouteStepId).HasColumnName("route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.RouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.VisitNo).HasColumnName("visit_no");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Instruction).HasColumnName("instruction");
            b.Property(x => x.EnteredAt).HasColumnName("entered_at");
            b.Property(x => x.ExitedAt).HasColumnName("exited_at");
            b.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at");
            b.Property(x => x.ConfirmedById).HasColumnName("confirmed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ConfirmedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RequiresConfirmation).HasColumnName("requires_confirmation");
            b.Property(x => x.AssignedById).HasColumnName("assigned_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.AssignedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.AssignedAt).HasColumnName("assigned_at");
            b.Property(x => x.AssignedEquipmentId).HasColumnName("assigned_equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.AssignedEquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.AssignedRecipeVersionId).HasColumnName("assigned_recipe_version_id");
            b.HasOne<RecipeVersion>().WithMany().HasForeignKey(x => x.AssignedRecipeVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.PassedQty).HasColumnName("passed_qty");
            b.Property(x => x.ScrapQty).HasColumnName("scrap_qty");
            b.HasIndex(x => new { x.LotId, x.RouteStepId, x.VisitNo }).IsUnique();
        });

        modelBuilder.Entity<ProcessRun>(b =>
        {
            b.ToTable("process_runs", "production", t =>
            {
                t.HasCheckConstraint("ck_process_runs_1", "track_out_at IS NULL OR track_out_at >= track_in_at");
                t.HasCheckConstraint("ck_process_runs_2", "status IN ('Running', 'Completed', 'Aborted')");
                t.HasCheckConstraint("ck_process_runs_3", "(status = 'Running' AND track_out_at IS NULL AND track_out_by_id IS NULL) OR (status <> 'Running' AND track_out_at IS NOT NULL AND track_out_by_id IS NOT NULL)");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            b.HasOne<StepExecution>()
                .WithMany()
                .HasForeignKey(x => x.StepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ChamberId).HasColumnName("chamber_id");
            b.HasOne<Chamber>().WithMany().HasForeignKey(x => x.ChamberId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RecipeVersionId).HasColumnName("recipe_version_id");
            b.Property(x => x.RecipeApplicabilityId).HasColumnName("recipe_applicability_id");
            b.HasOne<RecipeApplicability>().WithMany().HasForeignKey(x => x.RecipeApplicabilityId)
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<RecipeVersion>()
                .WithMany()
                .HasForeignKey(x => x.RecipeVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.TrackInAt).HasColumnName("track_in_at");
            b.Property(x => x.TrackOutAt).HasColumnName("track_out_at");
            b.Property(x => x.TrackInById).HasColumnName("track_in_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.TrackInById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.TrackOutById).HasColumnName("track_out_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.TrackOutById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
        });

        modelBuilder.Entity<ProcessRunWafer>(b =>
        {
            b.ToTable("process_run_wafers", "production", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            b.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WaferId).HasColumnName("wafer_id");
            b.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OperatorId).HasColumnName("operator_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Result).HasColumnName("result").HasMaxLength(32);
            b.Property(x => x.Parameters).HasColumnName("parameters").HasColumnType("jsonb");
            b.Property(x => x.Notes).HasColumnName("notes");
            b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
            b.HasIndex(x => new { x.ProcessRunId, x.WaferId }).IsUnique();
        });

        modelBuilder.Entity<TrackEvent>(b =>
        {
            b.ToTable("track_events", "production", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            b.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(32);
            b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            b.Property(x => x.RecordedAt).HasColumnName("recorded_at");
            b.Property(x => x.ActorId).HasColumnName("actor_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Source).HasColumnName("source").HasMaxLength(32);
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            b.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
            b.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
            b.HasIndex(x => x.IdempotencyKey).IsUnique();
        });

        modelBuilder.Entity<ProcessConfirmation>(b =>
        {
            b.ToTable("process_confirmations", "production", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            b.HasOne<StepExecution>()
                .WithMany()
                .HasForeignKey(x => x.StepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ConfirmedById).HasColumnName("confirmed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ConfirmedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at");
            b.Property(x => x.ConfirmationType).HasColumnName("confirmation_type").HasMaxLength(64);
            b.Property(x => x.Notes).HasColumnName("notes");
            b.Property(x => x.Value).HasColumnName("value").HasColumnType("jsonb");
        });

        modelBuilder.Entity<OutputValue>(b =>
        {
            b.ToTable("output_values", "production", t =>
            {
                t.HasCheckConstraint("ck_output_values_1", "num_nonnulls(value_text, value_number, value_boolean, value_date) = 1");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            b.HasOne<StepExecution>()
                .WithMany()
                .HasForeignKey(x => x.StepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OutputId).HasColumnName("output_id");
            b.HasOne<OutputDefinition>().WithMany().HasForeignKey(x => x.OutputId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ValueText).HasColumnName("value_text");
            b.Property(x => x.ValueNumber).HasColumnName("value_number").HasPrecision(20, 6);
            b.Property(x => x.ValueBoolean).HasColumnName("value_boolean");
            b.Property(x => x.ValueDate).HasColumnName("value_date");
            b.Property(x => x.RecordedById).HasColumnName("recorded_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.RecordedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RecordedAt).HasColumnName("recorded_at");
            b.HasIndex(x => new { x.StepExecutionId, x.OutputId }).IsUnique();
        });

        modelBuilder.Entity<ChecklistCompletion>(b =>
        {
            b.ToTable("checklist_completions", "production", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            b.HasOne<StepExecution>()
                .WithMany()
                .HasForeignKey(x => x.StepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ChecklistItemId).HasColumnName("checklist_item_id");
            b.HasOne<ChecklistItem>()
                .WithMany()
                .HasForeignKey(x => x.ChecklistItemId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CheckedById).HasColumnName("checked_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.CheckedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CheckedAt).HasColumnName("checked_at");
            b.HasIndex(x => new { x.StepExecutionId, x.ChecklistItemId }).IsUnique();
        });

        modelBuilder.Entity<StepDocument>(b =>
        {
            b.ToTable("step_documents", "production", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            b.HasOne<StepExecution>()
                .WithMany()
                .HasForeignKey(x => x.StepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EngineeringDocumentId).HasColumnName("engineering_document_id");
            b.HasOne<EngineeringDocument>()
                .WithMany()
                .HasForeignKey(x => x.EngineeringDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Reference).HasColumnName("reference");
            b.Property(x => x.FilePath).HasColumnName("file_path");
            b.Property(x => x.IsMandatory).HasColumnName("is_mandatory");
            b.Property(x => x.RequiresValidation).HasColumnName("requires_validation");
            b.Property(x => x.ValidatedAt).HasColumnName("validated_at");
            b.Property(x => x.ValidatedById).HasColumnName("validated_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ValidatedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkOrderStop>(b =>
        {
            b.ToTable("work_order_stops", "production", t =>
            {
                t.HasCheckConstraint("ck_work_order_stops_1", "resumed_at IS NULL OR resumed_at >= stopped_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
            b.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StopType).HasColumnName("stop_type").HasMaxLength(32);
            b.Property(x => x.Reason).HasColumnName("reason");
            b.Property(x => x.IssueId).HasColumnName("issue_id");
            b.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StoppedById).HasColumnName("stopped_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.StoppedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StoppedAt).HasColumnName("stopped_at");
            b.Property(x => x.ResumedById).HasColumnName("resumed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ResumedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ResumedAt).HasColumnName("resumed_at");
            b.Property(x => x.ResumeNotes).HasColumnName("resume_notes");
            b.Property(x => x.Context).HasColumnName("context").HasColumnType("jsonb");
        });

        modelBuilder.Entity<ProcessBatch>(b =>
        {
            b.ToTable("process_batches", "production", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<ProcessBatchMember>(b =>
        {
            b.ToTable("process_batch_members", "production", t =>
            {
                t.HasCheckConstraint("ck_process_batch_members_1", "position > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.ProcessBatchId).HasColumnName("process_batch_id");
            b.HasOne<ProcessBatch>().WithMany().HasForeignKey(x => x.ProcessBatchId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            b.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WaferId).HasColumnName("wafer_id");
            b.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Position).HasColumnName("position");
            b.HasIndex(x => new { x.ProcessBatchId, x.WaferId }).IsUnique();
            b.HasIndex(x => new { x.ProcessBatchId, x.Position }).IsUnique();
        });

        modelBuilder.Entity<QTimeWindow>(b =>
        {
            b.ToTable("q_time_windows", "production", t =>
            {
                t.HasCheckConstraint("ck_q_time_windows_1", "deadline_at > started_at");
                t.HasCheckConstraint("ck_q_time_windows_2", "ended_at IS NULL OR ended_at >= started_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.QTimeRuleId).HasColumnName("q_time_rule_id");
            b.HasOne<QTimeRule>().WithMany().HasForeignKey(x => x.QTimeRuleId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StartedAt).HasColumnName("started_at");
            b.Property(x => x.DeadlineAt).HasColumnName("deadline_at");
            b.Property(x => x.EndedAt).HasColumnName("ended_at");
            b.Property(x => x.ViolatedAt).HasColumnName("violated_at");
        });

        modelBuilder.Entity<ReworkRecord>(b =>
        {
            b.ToTable("rework_records", "production", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OriginalStepExecutionId).HasColumnName("original_step_execution_id");
            b.HasOne<StepExecution>()
                .WithMany()
                .HasForeignKey(x => x.OriginalStepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReworkRouteVersionId).HasColumnName("rework_route_version_id");
            b.HasOne<RouteVersion>()
                .WithMany()
                .HasForeignKey(x => x.ReworkRouteVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReturnRouteStepId).HasColumnName("return_route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.ReturnRouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReasonCodeId).HasColumnName("reason_code_id");
            b.HasOne<ReasonCode>().WithMany().HasForeignKey(x => x.ReasonCodeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ApprovedById).HasColumnName("approved_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        });
    }
}
