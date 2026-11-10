using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureAudit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("audit_logs", "audit", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(64);
            b.Property(x => x.EntityId).HasColumnName("entity_id");
            b.Property(x => x.Action).HasColumnName("action").HasMaxLength(64);
            b.Property(x => x.BeforeState).HasColumnName("before_state").HasColumnType("jsonb");
            b.Property(x => x.AfterState).HasColumnName("after_state").HasColumnType("jsonb");
            b.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
            b.Property(x => x.UserAgent).HasColumnName("user_agent");
            b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            b.Property(x => x.SourceChannel).HasColumnName("source_channel").HasMaxLength(32);
            b.Property(x => x.ReasonCodeId).HasColumnName("reason_code_id");
            b.HasOne<ReasonCode>().WithMany().HasForeignKey(x => x.ReasonCodeId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<BusinessEvent>(b =>
        {
            b.ToTable("business_events", "audit", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(64);
            b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(64);
            b.Property(x => x.EntityId).HasColumnName("entity_id");
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
            b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        });
        modelBuilder.Entity<Attachment>(b =>
        {
            b.ToTable("attachments", "audit", t =>
            {
                t.HasCheckConstraint("ck_attachments_1", "file_size >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(64);
            b.Property(x => x.EntityId).HasColumnName("entity_id");
            b.Property(x => x.OriginalName).HasColumnName("original_name").HasMaxLength(255);
            b.Property(x => x.StoragePath).HasColumnName("storage_path");
            b.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(128);
            b.Property(x => x.FileSize).HasColumnName("file_size");
            b.Property(x => x.Checksum).HasColumnName("checksum").HasMaxLength(128);
            b.Property(x => x.UploadedById).HasColumnName("uploaded_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CommandReceipt>(b =>
        {
            b.ToTable("command_receipts", "audit", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Scope).HasColumnName("scope").HasMaxLength(128);
            b.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
            b.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(128);
            b.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.Result).HasColumnName("result").HasColumnType("jsonb");
            b.Property(x => x.CompletedAt).HasColumnName("completed_at");
            b.HasIndex(x => new { x.Scope, x.IdempotencyKey }).IsUnique();
        });
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages", "audit", t =>
            {
                t.HasCheckConstraint("ck_outbox_messages_1", "attempts >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(128);
            b.Property(x => x.AggregateType).HasColumnName("aggregate_type").HasMaxLength(64);
            b.Property(x => x.AggregateId).HasColumnName("aggregate_id");
            b.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
            b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            b.Property(x => x.PublishedAt).HasColumnName("published_at");
            b.Property(x => x.Attempts).HasColumnName("attempts");
            b.Property(x => x.NextAttemptAt).HasColumnName("next_attempt_at");
            b.Property(x => x.LastError).HasColumnName("last_error");
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        });
    }
}
