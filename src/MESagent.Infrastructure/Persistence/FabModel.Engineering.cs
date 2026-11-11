using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureEngineering(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("products", "engineering", t =>
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
            b.Property(x => x.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(16);
            b.Property(x => x.Category).HasColumnName("category").HasMaxLength(64);
            b.Property(x => x.ExternalCode).HasColumnName("external_code").HasMaxLength(128);
            b.Property(x => x.ExternalSystem).HasColumnName("external_system").HasMaxLength(64);
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Route>(b =>
        {
            b.ToTable("routes", "engineering", t =>
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
            b.Property(x => x.ProductId).HasColumnName("product_id");
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<RouteVersion>(b =>
        {
            b.ToTable("route_versions", "engineering", t =>
            {
                t.HasCheckConstraint("ck_route_versions_1", "effective_to IS NULL OR effective_from IS NULL OR effective_to > effective_from");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.RouteId).HasColumnName("route_id");
            b.HasOne<Route>().WithMany().HasForeignKey(x => x.RouteId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).HasColumnName("version").HasMaxLength(32);
            b.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(32);
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            b.Property(x => x.ReleasedAt).HasColumnName("released_at");
            b.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ChangeReason).HasColumnName("change_reason");
            b.HasIndex(x => new { x.RouteId, x.Version }).IsUnique();
        });

        modelBuilder.Entity<ProductRevision>(b =>
        {
            b.ToTable("product_revisions", "engineering", t =>
            {
                t.HasCheckConstraint("ck_product_revisions_1", "effective_to IS NULL OR effective_from IS NULL OR effective_to > effective_from");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.ProductId).HasColumnName("product_id");
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RevisionCode).HasColumnName("revision_code").HasMaxLength(32);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(32);
            b.Property(x => x.RouteVersionId).HasColumnName("route_version_id");
            b.HasOne<RouteVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ChangeReason).HasColumnName("change_reason");
            b.Property(x => x.ExternalRef).HasColumnName("external_ref").HasMaxLength(128);
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            b.Property(x => x.ReleasedAt).HasColumnName("released_at");
            b.Property(x => x.ObsoleteAt).HasColumnName("obsolete_at");
            b.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.ProductId, x.RevisionCode }).IsUnique();
        });

        modelBuilder.Entity<Operation>(b =>
        {
            b.ToTable("operations", "engineering", t =>
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
            b.Property(x => x.SegmentType).HasColumnName("segment_type").HasMaxLength(32);
            b.Property(x => x.EquipmentTypeId).HasColumnName("equipment_type_id");
            b.HasOne<EquipmentType>()
                .WithMany()
                .HasForeignKey(x => x.EquipmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StandardInstruction).HasColumnName("standard_instruction");
            b.Property(x => x.Parameters).HasColumnName("parameters").HasColumnType("jsonb");
            b.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<RouteStep>(b =>
        {
            b.ToTable("route_steps", "engineering", t =>
            {
                t.HasCheckConstraint("ck_route_steps_1", "sequence_no > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.RouteVersionId).HasColumnName("route_version_id");
            b.HasOne<RouteVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OperationId).HasColumnName("operation_id");
            b.HasOne<Operation>().WithMany().HasForeignKey(x => x.OperationId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.SequenceNo).HasColumnName("sequence_no");
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Instruction).HasColumnName("instruction");
            b.Property(x => x.EquipmentTypeId).HasColumnName("equipment_type_id");
            b.HasOne<EquipmentType>()
                .WithMany()
                .HasForeignKey(x => x.EquipmentTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RequiresConfirmation).HasColumnName("requires_confirmation");
            b.Property(x => x.Parameters).HasColumnName("parameters").HasColumnType("jsonb");
            b.HasIndex(x => new { x.RouteVersionId, x.SequenceNo }).IsUnique();
        });

        modelBuilder.Entity<Recipe>(b =>
        {
            b.ToTable("recipes", "engineering", t =>
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

        modelBuilder.Entity<RecipeVersion>(b =>
        {
            b.ToTable("recipe_versions", "engineering", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.RecipeId).HasColumnName("recipe_id");
            b.HasOne<Recipe>().WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Version).HasColumnName("version").HasMaxLength(32);
            b.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(32);
            b.Property(x => x.Checksum).HasColumnName("checksum").HasMaxLength(128);
            b.Property(x => x.ReleasedAt).HasColumnName("released_at");
            b.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Parameters).HasColumnName("parameters").HasColumnType("jsonb");
            b.HasIndex(x => new { x.RecipeId, x.Version }).IsUnique();
        });

        modelBuilder.Entity<RecipeApplicability>(b =>
        {
            b.ToTable("recipe_applicabilities", "engineering", t =>
            {
                t.HasCheckConstraint("ck_recipe_applicabilities_1", "effective_to IS NULL OR effective_to > effective_from");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.RecipeVersionId).HasColumnName("recipe_version_id");
            b.HasOne<RecipeVersion>()
                .WithMany()
                .HasForeignKey(x => x.RecipeVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RouteStepId).HasColumnName("route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.RouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
        });

        modelBuilder.Entity<NumberSequence>(b =>
        {
            b.ToTable("number_sequences", "engineering", t =>
            {
                t.HasCheckConstraint("ck_number_sequences_1", "next_number >= 0 AND pad_size >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.ProductId).HasColumnName("product_id");
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(32);
            b.Property(x => x.Prefix).HasColumnName("prefix").HasMaxLength(64);
            b.Property(x => x.Suffix).HasColumnName("suffix").HasMaxLength(64);
            b.Property(x => x.NextNumber).HasColumnName("next_number");
            b.Property(x => x.PadSize).HasColumnName("pad_size");
            b.Property(x => x.Pattern).HasColumnName("pattern").HasMaxLength(200);
            b.Property(x => x.ResetPeriod).HasColumnName("reset_period").HasMaxLength(32);
            b.Property(x => x.LastResetKey).HasColumnName("last_reset_key").HasMaxLength(64);
            b.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<OutputDefinition>(b =>
        {
            b.ToTable("output_definitions", "engineering", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.RouteStepId).HasColumnName("route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.RouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Key).HasColumnName("key").HasMaxLength(64);
            b.Property(x => x.Label).HasColumnName("label").HasMaxLength(200);
            b.Property(x => x.ValueType).HasColumnName("value_type").HasMaxLength(32);
            b.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(32);
            b.Property(x => x.Options).HasColumnName("options").HasColumnType("jsonb");
            b.Property(x => x.IsRequired).HasColumnName("is_required");
            b.Property(x => x.SortOrder).HasColumnName("sort_order");
            b.HasIndex(x => new { x.RouteStepId, x.Key }).IsUnique();
        });

        modelBuilder.Entity<ChecklistItem>(b =>
        {
            b.ToTable("checklist_items", "engineering", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.RouteStepId).HasColumnName("route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.RouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Label).HasColumnName("label").HasMaxLength(200);
            b.Property(x => x.IsRequired).HasColumnName("is_required");
            b.Property(x => x.SortOrder).HasColumnName("sort_order");
        });

        modelBuilder.Entity<EngineeringDocument>(b =>
        {
            b.ToTable("engineering_documents", "engineering", t =>
            {
                t.HasCheckConstraint("ck_engineering_documents_1", "file_size >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(64);
            b.Property(x => x.EntityId).HasColumnName("entity_id");
            b.Property(x => x.OriginalFilename).HasColumnName("original_filename").HasMaxLength(255);
            b.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(64);
            b.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(128);
            b.Property(x => x.FileSize).HasColumnName("file_size");
            b.Property(x => x.Revision).HasColumnName("revision").HasMaxLength(32);
            b.Property(x => x.Checksum).HasColumnName("checksum").HasMaxLength(128);
            b.Property(x => x.StoragePath).HasColumnName("storage_path");
            b.Property(x => x.LifecycleStatus).HasColumnName("lifecycle_status").HasMaxLength(32);
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            b.Property(x => x.ReleasedAt).HasColumnName("released_at");
            b.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.UploadedById).HasColumnName("uploaded_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QTimeRule>(b =>
        {
            b.ToTable("q_time_rules", "engineering", t =>
            {
                t.HasCheckConstraint("ck_q_time_rules_1", "limit_minutes > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.StartRouteStepId).HasColumnName("start_route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.StartRouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EndRouteStepId).HasColumnName("end_route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.EndRouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StartEventType).HasColumnName("start_event_type").HasMaxLength(32);
            b.Property(x => x.EndEventType).HasColumnName("end_event_type").HasMaxLength(32);
            b.Property(x => x.LimitMinutes).HasColumnName("limit_minutes").HasPrecision(20, 6);
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        });
    }
}
