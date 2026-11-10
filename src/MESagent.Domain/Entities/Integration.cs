namespace MESagent.Domain.Entities;

/// <summary>Integration / integration_configs，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class IntegrationConfig
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string SystemType { get; set; } = null!;
    public string SystemName { get; set; } = null!;
    public string? Config { get; set; }
    public string? SecretReference { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Integration / machine_connections，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MachineConnection
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid EquipmentId { get; set; }
    public string Name { get; set; } = null!;
    public string Protocol { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public string Status { get; set; } = null!;
    public string? StatusMessage { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public string? Config { get; set; }
    public string? SecretReference { get; set; }
}

/// <summary>Integration / machine_messages，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MachineMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid MachineConnectionId { get; set; }
    public string? Topic { get; set; }
    public string RawPayload { get; set; } = null!;
    public string? ParsedData { get; set; }
    public string ProcessingStatus { get; set; } = null!;
    public string? ProcessingError { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string? SourceMessageId { get; set; }
}

/// <summary>Integration / machine_tags，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MachineTag
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid MachineConnectionId { get; set; }
    public Guid EquipmentId { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string SignalType { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public string? Transform { get; set; }
    public string? Unit { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Integration / api_keys，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ApiKey
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid IntegrationConfigId { get; set; }
    public string Name { get; set; } = null!;
    public string KeyPrefix { get; set; } = null!;
    public string KeyHash { get; set; } = null!;
    public string Scopes { get; set; } = null!;
    public string? IpAllowlist { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>Integration / import_mappings，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ImportMapping
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Name { get; set; } = null!;
    public string MappingConfig { get; set; } = null!;
    public string Entity { get; set; } = null!;
}

/// <summary>Integration / import_jobs，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ImportJob
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid UserId { get; set; }
    public string Entity { get; set; } = null!;
    public Guid? MappingId { get; set; }
    public string OriginalFilename { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorLog { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool DryRun { get; set; }
}

/// <summary>Integration / label_templates，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class LabelTemplate
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Name { get; set; } = null!;
    public string LabelType { get; set; } = null!;
    public string Size { get; set; } = null!;
    public string FieldsConfig { get; set; } = null!;
    public string BarcodeFormat { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

/// <summary>Integration / custom_field_definitions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class CustomFieldDefinition
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string EntityType { get; set; } = null!;
    public string Key { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string FieldType { get; set; } = null!;
    public string? Config { get; set; }
    public bool Required { get; set; }
    public int Position { get; set; }
    public bool IsActive { get; set; } = true;
}
