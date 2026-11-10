namespace MESagent.Domain.Entities;

/// <summary>Engineering / products，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Product
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string UnitOfMeasure { get; set; } = null!;
    public string? Category { get; set; }
    public string? ExternalCode { get; set; }
    public string? ExternalSystem { get; set; }
}

/// <summary>Engineering / routes，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Route
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? ProductId { get; set; }
}

/// <summary>Engineering / route_versions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class RouteVersion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid RouteId { get; set; }
    public string Version { get; set; } = null!;
    public string LifecycleStatus { get; set; } = null!;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
    public string? ChangeReason { get; set; }
}

/// <summary>Engineering / product_revisions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ProductRevision
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid ProductId { get; set; }
    public string RevisionCode { get; set; } = null!;
    public string? Description { get; set; }
    public string LifecycleStatus { get; set; } = null!;
    public Guid? RouteVersionId { get; set; }
    public string? ChangeReason { get; set; }
    public string? ExternalRef { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime? ObsoleteAt { get; set; }
    public Guid? ReleasedById { get; set; }
}

/// <summary>Engineering / operations，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Operation
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string SegmentType { get; set; } = null!;
    public Guid? EquipmentTypeId { get; set; }
    public string? StandardInstruction { get; set; }
    public string? Parameters { get; set; }
}

/// <summary>Engineering / route_steps，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class RouteStep
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid RouteVersionId { get; set; }
    public Guid OperationId { get; set; }
    public int SequenceNo { get; set; }
    public string Name { get; set; } = null!;
    public string? Instruction { get; set; }
    public Guid? EquipmentTypeId { get; set; }
    public bool RequiresConfirmation { get; set; }
    public string? Parameters { get; set; }
}

/// <summary>Engineering / recipes，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Recipe
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Engineering / recipe_versions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class RecipeVersion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid RecipeId { get; set; }
    public string Version { get; set; } = null!;
    public string LifecycleStatus { get; set; } = null!;
    public string? Checksum { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
    public string? Parameters { get; set; }
}

/// <summary>Engineering / recipe_applicabilities，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class RecipeApplicability
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid RecipeVersionId { get; set; }
    public Guid RouteStepId { get; set; }
    public Guid EquipmentId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

/// <summary>Engineering / number_sequences，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class NumberSequence
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ProductId { get; set; }
    public string Purpose { get; set; } = null!;
    public string? Prefix { get; set; }
    public string? Suffix { get; set; }
    public long NextNumber { get; set; }
    public int PadSize { get; set; }
    public string? Pattern { get; set; }
    public string? ResetPeriod { get; set; }
    public string? LastResetKey { get; set; }
}

/// <summary>Engineering / output_definitions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class OutputDefinition
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid RouteStepId { get; set; }
    public string Key { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string ValueType { get; set; } = null!;
    public string? Unit { get; set; }
    public string? Options { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Engineering / checklist_items，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ChecklistItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid RouteStepId { get; set; }
    public string Label { get; set; } = null!;
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>Engineering / engineering_documents，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class EngineeringDocument
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string OriginalFilename { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long FileSize { get; set; }
    public string Revision { get; set; } = null!;
    public string Checksum { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public string LifecycleStatus { get; set; } = null!;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
    public Guid UploadedById { get; set; }
}

/// <summary>Engineering / q_time_rules，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class QTimeRule
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid StartRouteStepId { get; set; }
    public Guid EndRouteStepId { get; set; }
    public string StartEventType { get; set; } = null!;
    public string EndEventType { get; set; } = null!;
    public decimal LimitMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
