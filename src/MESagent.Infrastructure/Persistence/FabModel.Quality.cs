using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureQuality(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReasonCode>(b =>
        {
            b.ToTable("reason_codes", "quality", t =>
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
            b.Property(x => x.Category).HasColumnName("category").HasMaxLength(32);
            b.HasIndex(x => new { x.Category, x.Code }).IsUnique();
        });

        modelBuilder.Entity<IssueType>(b =>
        {
            b.ToTable("issue_types", "quality", t =>
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
            b.Property(x => x.Severity).HasColumnName("severity").HasMaxLength(32);
            b.Property(x => x.IsBlocking).HasColumnName("is_blocking");
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Issue>(b =>
        {
            b.ToTable("issues", "quality", t =>
            {
                t.HasCheckConstraint("ck_issues_1", "non_conforming_qty IS NULL OR non_conforming_qty >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
            b.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            b.HasOne<StepExecution>()
                .WithMany()
                .HasForeignKey(x => x.StepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WaferId).HasColumnName("wafer_id");
            b.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.IssueTypeId).HasColumnName("issue_type_id");
            b.HasOne<IssueType>().WithMany().HasForeignKey(x => x.IssueTypeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.ReportedById).HasColumnName("reported_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReportedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.AssignedToId).HasColumnName("assigned_to_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReportedAt).HasColumnName("reported_at");
            b.Property(x => x.AcknowledgedAt).HasColumnName("acknowledged_at");
            b.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
            b.Property(x => x.ClosedAt).HasColumnName("closed_at");
            b.Property(x => x.ResolutionNotes).HasColumnName("resolution_notes");
            b.Property(x => x.Disposition).HasColumnName("disposition").HasMaxLength(32);
            b.Property(x => x.NonConformingQty).HasColumnName("non_conforming_qty");
            b.Property(x => x.RootCause).HasColumnName("root_cause");
            b.Property(x => x.ContainmentAction).HasColumnName("containment_action");
            b.Property(x => x.DispositionById).HasColumnName("disposition_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.DispositionById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.DispositionAt).HasColumnName("disposition_at");
        });

        modelBuilder.Entity<IssueAction>(b =>
        {
            b.ToTable("issue_actions", "quality", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.IssueId).HasColumnName("issue_id");
            b.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ActionType).HasColumnName("action_type").HasMaxLength(32);
            b.Property(x => x.Title).HasColumnName("title").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.AssignedToId).HasColumnName("assigned_to_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.DueDate).HasColumnName("due_date");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.Property(x => x.CompletedById).HasColumnName("completed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.CompletedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.VerifiedAt).HasColumnName("verified_at");
            b.Property(x => x.VerifiedById).HasColumnName("verified_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.VerifiedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Notes).HasColumnName("notes");
        });

        modelBuilder.Entity<HoldRecord>(b =>
        {
            b.ToTable("hold_records", "quality", t =>
            {
                t.HasCheckConstraint("ck_hold_records_1", "released_at IS NULL OR released_at >= held_at");
                t.HasCheckConstraint("ck_hold_records_2", "(released_at IS NULL AND released_by_id IS NULL AND release_reason IS NULL) OR (released_at IS NOT NULL AND released_by_id IS NOT NULL AND release_reason IS NOT NULL)");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReasonCodeId).HasColumnName("reason_code_id");
            b.HasOne<ReasonCode>().WithMany().HasForeignKey(x => x.ReasonCodeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.IssueId).HasColumnName("issue_id");
            b.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.HoldScope).HasColumnName("hold_scope").HasMaxLength(32);
            b.Property(x => x.HeldAt).HasColumnName("held_at");
            b.Property(x => x.HeldById).HasColumnName("held_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.HeldById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReleasedAt).HasColumnName("released_at");
            b.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReleaseReason).HasColumnName("release_reason");
        });

        modelBuilder.Entity<MaterialHold>(b =>
        {
            b.ToTable("material_holds", "quality", t =>
            {
                t.HasCheckConstraint("ck_material_holds_1", "released_at IS NULL OR released_at >= held_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.MaterialLotId).HasColumnName("material_lot_id");
            b.HasOne<MaterialLot>().WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReasonCodeId).HasColumnName("reason_code_id");
            b.HasOne<ReasonCode>().WithMany().HasForeignKey(x => x.ReasonCodeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.IssueId).HasColumnName("issue_id");
            b.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.HeldAt).HasColumnName("held_at");
            b.Property(x => x.HeldById).HasColumnName("held_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.HeldById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReleasedAt).HasColumnName("released_at");
            b.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReleaseReason).HasColumnName("release_reason");
        });

        modelBuilder.Entity<LotRelease>(b =>
        {
            b.ToTable("lot_releases", "quality", t =>
            {
                t.HasCheckConstraint("ck_lot_releases_1", "release_type IN ('Production', 'Quality')");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReleaseType).HasColumnName("release_type").HasMaxLength(32);
            b.Property(x => x.ReleasedAt).HasColumnName("released_at");
            b.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Reason).HasColumnName("reason");
            b.Property(x => x.IssueId).HasColumnName("issue_id");
            b.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InspectionPlan>(b =>
        {
            b.ToTable("inspection_plans", "quality", t =>
            {
                t.HasCheckConstraint("ck_inspection_plans_1", "version > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.MaterialId).HasColumnName("material_id");
            b.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialTypeId).HasColumnName("material_type_id");
            b.HasOne<MaterialType>().WithMany().HasForeignKey(x => x.MaterialTypeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Criteria).HasColumnName("criteria").HasColumnType("jsonb");
            b.Property(x => x.Version).HasColumnName("version");
            b.Property(x => x.PublishedAt).HasColumnName("published_at");
            b.Property(x => x.RootId).HasColumnName("root_id");
            b.HasOne<InspectionPlan>().WithMany().HasForeignKey(x => x.RootId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        });

        modelBuilder.Entity<IncomingInspection>(b =>
        {
            b.ToTable("incoming_inspections", "quality", t =>
            {
                t.HasCheckConstraint("ck_incoming_inspections_1", "quantity_received >= 0 AND plan_version > 0");
                t.HasCheckConstraint("ck_incoming_inspections_2", "completed_at IS NULL OR completed_at >= started_at");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.InspectionPlanId).HasColumnName("inspection_plan_id");
            b.HasOne<InspectionPlan>()
                .WithMany()
                .HasForeignKey(x => x.InspectionPlanId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialId).HasColumnName("material_id");
            b.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialLotId).HasColumnName("material_lot_id");
            b.HasOne<MaterialLot>().WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotNumber).HasColumnName("lot_number").HasMaxLength(64);
            b.Property(x => x.SupplierLotRef).HasColumnName("supplier_lot_ref").HasMaxLength(128);
            b.Property(x => x.QuantityReceived).HasColumnName("quantity_received").HasPrecision(20, 6);
            b.Property(x => x.InspectorId).HasColumnName("inspector_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.InspectorId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StartedAt).HasColumnName("started_at");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.Disposition).HasColumnName("disposition").HasMaxLength(32);
            b.Property(x => x.DispositionNotes).HasColumnName("disposition_notes");
            b.Property(x => x.DispositionById).HasColumnName("disposition_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.DispositionById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.DispositionAt).HasColumnName("disposition_at");
            b.Property(x => x.PlanVersion).HasColumnName("plan_version");
            b.Property(x => x.IssueId).HasColumnName("issue_id");
            b.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InspectionResult>(b =>
        {
            b.ToTable("inspection_results", "quality", t =>
            {
                t.HasCheckConstraint("ck_inspection_results_1", "spec_min IS NULL OR spec_max IS NULL OR spec_max >= spec_min");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.InspectionId).HasColumnName("inspection_id");
            b.HasOne<IncomingInspection>()
                .WithMany()
                .HasForeignKey(x => x.InspectionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CriterionName).HasColumnName("criterion_name").HasMaxLength(200);
            b.Property(x => x.CriterionType).HasColumnName("criterion_type").HasMaxLength(32);
            b.Property(x => x.Required).HasColumnName("required");
            b.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(32);
            b.Property(x => x.SpecMin).HasColumnName("spec_min").HasPrecision(20, 6);
            b.Property(x => x.SpecMax).HasColumnName("spec_max").HasPrecision(20, 6);
            b.Property(x => x.ValueNumeric).HasColumnName("value_numeric").HasPrecision(20, 6);
            b.Property(x => x.ValueBoolean).HasColumnName("value_boolean");
            b.Property(x => x.ValueText).HasColumnName("value_text");
            b.Property(x => x.IsPassed).HasColumnName("is_passed");
            b.Property(x => x.Notes).HasColumnName("notes");
        });

        modelBuilder.Entity<QualityCheckTemplate>(b =>
        {
            b.ToTable("quality_check_templates", "quality", t =>
            {
                t.HasCheckConstraint("ck_quality_check_templates_1", "version > 0 AND min_checks_per_batch >= 0 AND samples_per_check > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.RouteVersionId).HasColumnName("route_version_id");
            b.HasOne<RouteVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RouteStepId).HasColumnName("route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.RouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Version).HasColumnName("version");
            b.Property(x => x.MinChecksPerBatch).HasColumnName("min_checks_per_batch");
            b.Property(x => x.SamplesPerCheck).HasColumnName("samples_per_check");
            b.Property(x => x.Parameters).HasColumnName("parameters").HasColumnType("jsonb");
            b.HasIndex(x => new { x.RouteVersionId, x.Name, x.Version }).IsUnique();
        });

        modelBuilder.Entity<SpecificationVersion>(b =>
        {
            b.ToTable("specification_versions", "quality", t =>
            {
                t.HasCheckConstraint("ck_specification_versions_1", "version > 0");
                t.HasCheckConstraint("ck_specification_versions_2", "spec_min IS NULL OR spec_max IS NULL OR spec_max >= spec_min");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.ParameterCode).HasColumnName("parameter_code").HasMaxLength(64);
            b.Property(x => x.Version).HasColumnName("version");
            b.Property(x => x.ValueType).HasColumnName("value_type").HasMaxLength(32);
            b.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(32);
            b.Property(x => x.SpecMin).HasColumnName("spec_min").HasPrecision(20, 6);
            b.Property(x => x.SpecMax).HasColumnName("spec_max").HasPrecision(20, 6);
            b.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(32);
            b.Property(x => x.ReleasedAt).HasColumnName("released_at");
            b.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.ParameterCode, x.Version }).IsUnique();
        });

        modelBuilder.Entity<QualityCheck>(b =>
        {
            b.ToTable("quality_checks", "quality", t =>
            {
                t.HasCheckConstraint("ck_quality_checks_1", "production_quantity >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            b.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.QualityCheckTemplateId).HasColumnName("quality_check_template_id");
            b.HasOne<QualityCheckTemplate>()
                .WithMany()
                .HasForeignKey(x => x.QualityCheckTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CheckedById).HasColumnName("checked_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.CheckedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CheckedAt).HasColumnName("checked_at");
            b.Property(x => x.ProductionQuantity).HasColumnName("production_quantity");
            b.Property(x => x.AllPassed).HasColumnName("all_passed");
            b.Property(x => x.Notes).HasColumnName("notes");
        });

        modelBuilder.Entity<Measurement>(b =>
        {
            b.ToTable("measurements", "quality", t =>
            {
                t.HasCheckConstraint("ck_measurements_1", "sample_number > 0");
                t.HasCheckConstraint("ck_measurements_2", "spec_min IS NULL OR spec_max IS NULL OR spec_max >= spec_min");
                t.HasCheckConstraint("ck_measurements_3", "num_nonnulls(value_numeric, value_boolean, value_text) = 1");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.QualityCheckId).HasColumnName("quality_check_id");
            b.HasOne<QualityCheck>().WithMany().HasForeignKey(x => x.QualityCheckId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            b.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WaferId).HasColumnName("wafer_id");
            b.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.SampleNumber).HasColumnName("sample_number");
            b.Property(x => x.SamplePoint).HasColumnName("sample_point").HasMaxLength(64);
            b.Property(x => x.SpecificationVersionId).HasColumnName("specification_version_id");
            b.HasOne<SpecificationVersion>()
                .WithMany()
                .HasForeignKey(x => x.SpecificationVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ParameterCode).HasColumnName("parameter_code").HasMaxLength(64);
            b.Property(x => x.ParameterType).HasColumnName("parameter_type").HasMaxLength(32);
            b.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(32);
            b.Property(x => x.SpecMin).HasColumnName("spec_min").HasPrecision(20, 6);
            b.Property(x => x.SpecMax).HasColumnName("spec_max").HasPrecision(20, 6);
            b.Property(x => x.ValueNumeric).HasColumnName("value_numeric").HasPrecision(20, 6);
            b.Property(x => x.ValueBoolean).HasColumnName("value_boolean");
            b.Property(x => x.ValueText).HasColumnName("value_text");
            b.Property(x => x.IsPassed).HasColumnName("is_passed");
            b.Property(x => x.MeasuredAt).HasColumnName("measured_at");
            b.Property(x => x.MeasurementEquipmentId).HasColumnName("measurement_equipment_id");
            b.HasOne<Equipment>()
                .WithMany()
                .HasForeignKey(x => x.MeasurementEquipmentId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RecordedById).HasColumnName("recorded_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.RecordedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QualityTrigger>(b =>
        {
            b.ToTable("quality_triggers", "quality", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.TriggerType).HasColumnName("trigger_type").HasMaxLength(32);
            b.Property(x => x.QualityCheckTemplateId).HasColumnName("quality_check_template_id");
            b.HasOne<QualityCheckTemplate>()
                .WithMany()
                .HasForeignKey(x => x.QualityCheckTemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProductId).HasColumnName("product_id");
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ThresholdN).HasColumnName("threshold_n");
            b.Property(x => x.DowntimeMinMinutes).HasColumnName("downtime_min_minutes").HasPrecision(20, 6);
            b.Property(x => x.IsBlocking).HasColumnName("is_blocking");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.Version).HasColumnName("version");
        });

        modelBuilder.Entity<QualityTask>(b =>
        {
            b.ToTable("quality_tasks", "quality", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.QualityTriggerId).HasColumnName("quality_trigger_id");
            b.HasOne<QualityTrigger>()
                .WithMany()
                .HasForeignKey(x => x.QualityTriggerId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.DueReason).HasColumnName("due_reason");
            b.Property(x => x.QualityCheckId).HasColumnName("quality_check_id");
            b.HasOne<QualityCheck>().WithMany().HasForeignKey(x => x.QualityCheckId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.IssueId).HasColumnName("issue_id");
            b.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.FiredAt).HasColumnName("fired_at");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.Property(x => x.CompletedById).HasColumnName("completed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.CompletedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScrapEntry>(b =>
        {
            b.ToTable("scrap_entries", "quality", t =>
            {
                t.HasCheckConstraint("ck_scrap_entries_1", "quantity > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
            b.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            b.HasOne<StepExecution>()
                .WithMany()
                .HasForeignKey(x => x.StepExecutionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReasonCodeId).HasColumnName("reason_code_id");
            b.HasOne<ReasonCode>().WithMany().HasForeignKey(x => x.ReasonCodeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Quantity).HasColumnName("quantity");
            b.Property(x => x.ShiftId).HasColumnName("shift_id");
            b.HasOne<Shift>().WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Notes).HasColumnName("notes");
            b.Property(x => x.ReportedById).HasColumnName("reported_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReportedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReportedAt).HasColumnName("reported_at");
        });

        modelBuilder.Entity<WaferDisposition>(b =>
        {
            b.ToTable("wafer_dispositions", "quality", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.WaferId).HasColumnName("wafer_id");
            b.HasOne<Wafer>().WithMany().HasForeignKey(x => x.WaferId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotIdAtEvent).HasColumnName("lot_id_at_event");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotIdAtEvent).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            b.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ScrapEntryId).HasColumnName("scrap_entry_id");
            b.HasOne<ScrapEntry>().WithMany().HasForeignKey(x => x.ScrapEntryId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.DispositionType).HasColumnName("disposition_type").HasMaxLength(32);
            b.Property(x => x.ReasonCodeId).HasColumnName("reason_code_id");
            b.HasOne<ReasonCode>().WithMany().HasForeignKey(x => x.ReasonCodeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.IssueId).HasColumnName("issue_id");
            b.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ApprovedById).HasColumnName("approved_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.PerformedById).HasColumnName("performed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.PerformedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        });
    }
}
