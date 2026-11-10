using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureIntegration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationConfig>(b =>
        {
            b.ToTable("integration_configs", "integration", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.SystemType).HasColumnName("system_type").HasMaxLength(32);
            b.Property(x => x.SystemName).HasColumnName("system_name").HasMaxLength(128);
            b.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb");
            b.Property(x => x.SecretReference).HasColumnName("secret_reference").HasMaxLength(255);
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.HasIndex(x => x.SystemName).IsUnique();
        });
        modelBuilder.Entity<MachineConnection>(b =>
        {
            b.ToTable("machine_connections", "integration", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Protocol).HasColumnName("protocol").HasMaxLength(32);
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.StatusMessage).HasColumnName("status_message");
            b.Property(x => x.LastConnectedAt).HasColumnName("last_connected_at");
            b.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb");
            b.Property(x => x.SecretReference).HasColumnName("secret_reference").HasMaxLength(255);
        });
        modelBuilder.Entity<MachineMessage>(b =>
        {
            b.ToTable("machine_messages", "integration", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.MachineConnectionId).HasColumnName("machine_connection_id");
            b.HasOne<MachineConnection>().WithMany().HasForeignKey(x => x.MachineConnectionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(255);
            b.Property(x => x.RawPayload).HasColumnName("raw_payload");
            b.Property(x => x.ParsedData).HasColumnName("parsed_data").HasColumnType("jsonb");
            b.Property(x => x.ProcessingStatus).HasColumnName("processing_status").HasMaxLength(32);
            b.Property(x => x.ProcessingError).HasColumnName("processing_error");
            b.Property(x => x.ReceivedAt).HasColumnName("received_at");
            b.Property(x => x.SourceMessageId).HasColumnName("source_message_id").HasMaxLength(128);
        });
        modelBuilder.Entity<MachineTag>(b =>
        {
            b.ToTable("machine_tags", "integration", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.MachineConnectionId).HasColumnName("machine_connection_id");
            b.HasOne<MachineConnection>().WithMany().HasForeignKey(x => x.MachineConnectionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EquipmentId).HasColumnName("equipment_id");
            b.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Address).HasColumnName("address").HasMaxLength(255);
            b.Property(x => x.SignalType).HasColumnName("signal_type").HasMaxLength(32);
            b.Property(x => x.DataType).HasColumnName("data_type").HasMaxLength(32);
            b.Property(x => x.Transform).HasColumnName("transform").HasColumnType("jsonb");
            b.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(32);
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.HasIndex(x => new { x.MachineConnectionId, x.Name }).IsUnique();
        });
        modelBuilder.Entity<ApiKey>(b =>
        {
            b.ToTable("api_keys", "integration", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.IntegrationConfigId).HasColumnName("integration_config_id");
            b.HasOne<IntegrationConfig>().WithMany().HasForeignKey(x => x.IntegrationConfigId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.KeyPrefix).HasColumnName("key_prefix").HasMaxLength(32);
            b.Property(x => x.KeyHash).HasColumnName("key_hash").HasMaxLength(128);
            b.Property(x => x.Scopes).HasColumnName("scopes").HasColumnType("jsonb");
            b.Property(x => x.IpAllowlist).HasColumnName("ip_allowlist").HasColumnType("jsonb");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.LastUsedAt).HasColumnName("last_used_at");
            b.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            b.HasIndex(x => x.KeyHash).IsUnique();
        });
        modelBuilder.Entity<ImportMapping>(b =>
        {
            b.ToTable("import_mappings", "integration", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.MappingConfig).HasColumnName("mapping_config").HasColumnType("jsonb");
            b.Property(x => x.Entity).HasColumnName("entity").HasMaxLength(64);
        });
        modelBuilder.Entity<ImportJob>(b =>
        {
            b.ToTable("import_jobs", "integration", t =>
            {
                t.HasCheckConstraint("ck_import_jobs_1", "total_rows >= 0 AND successful_rows >= 0 AND failed_rows >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Entity).HasColumnName("entity").HasMaxLength(64);
            b.Property(x => x.MappingId).HasColumnName("mapping_id");
            b.HasOne<ImportMapping>().WithMany().HasForeignKey(x => x.MappingId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.OriginalFilename).HasColumnName("original_filename").HasMaxLength(255);
            b.Property(x => x.FilePath).HasColumnName("file_path");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.TotalRows).HasColumnName("total_rows");
            b.Property(x => x.SuccessfulRows).HasColumnName("successful_rows");
            b.Property(x => x.FailedRows).HasColumnName("failed_rows");
            b.Property(x => x.ErrorLog).HasColumnName("error_log").HasColumnType("jsonb");
            b.Property(x => x.StartedAt).HasColumnName("started_at");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.Property(x => x.DryRun).HasColumnName("dry_run");
        });
        modelBuilder.Entity<LabelTemplate>(b =>
        {
            b.ToTable("label_templates", "integration", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.LabelType).HasColumnName("label_type").HasMaxLength(32);
            b.Property(x => x.Size).HasColumnName("size").HasColumnType("jsonb");
            b.Property(x => x.FieldsConfig).HasColumnName("fields_config").HasColumnType("jsonb");
            b.Property(x => x.BarcodeFormat).HasColumnName("barcode_format").HasMaxLength(32);
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        });
        modelBuilder.Entity<CustomFieldDefinition>(b =>
        {
            b.ToTable("custom_field_definitions", "integration", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(64);
            b.Property(x => x.Key).HasColumnName("key").HasMaxLength(64);
            b.Property(x => x.Label).HasColumnName("label").HasMaxLength(200);
            b.Property(x => x.FieldType).HasColumnName("field_type").HasMaxLength(32);
            b.Property(x => x.Config).HasColumnName("config").HasColumnType("jsonb");
            b.Property(x => x.Required).HasColumnName("required");
            b.Property(x => x.Position).HasColumnName("position");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.HasIndex(x => new { x.EntityType, x.Key }).IsUnique();
        });
    }
}
