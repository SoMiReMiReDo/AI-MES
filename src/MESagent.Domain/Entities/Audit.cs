namespace MESagent.Domain.Entities;

/// <summary>Audit / audit_logs，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class AuditLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid? UserId { get; set; }
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = null!;
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
    public string SourceChannel { get; set; } = null!;
    public Guid? ReasonCodeId { get; set; }
}

/// <summary>Audit / business_events，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class BusinessEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string EventType { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string Payload { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}

/// <summary>Audit / attachments，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Attachment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string OriginalName { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long FileSize { get; set; }
    public string Checksum { get; set; } = null!;
    public Guid UploadedById { get; set; }
}

/// <summary>Audit / command_receipts，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class CommandReceipt
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Scope { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public string RequestHash { get; set; } = null!;
    public Guid? ActorUserId { get; set; }
    public Guid CorrelationId { get; set; }
    public string Status { get; set; } = null!;
    public string? Result { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>Audit / outbox_messages，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string EventType { get; set; } = null!;
    public string AggregateType { get; set; } = null!;
    public Guid AggregateId { get; set; }
    public string Payload { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public Guid CorrelationId { get; set; }
}
