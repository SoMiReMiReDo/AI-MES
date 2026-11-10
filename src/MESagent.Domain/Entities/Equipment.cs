namespace MESagent.Domain.Entities;

/// <summary>Equipment / equipment_types，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class EquipmentType
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

/// <summary>Equipment / equipment_groups，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class EquipmentGroup
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid FabId { get; set; }
}

/// <summary>Equipment / equipment，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Equipment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid AreaId { get; set; }
    public Guid? LineId { get; set; }
    public Guid EquipmentGroupId { get; set; }
    public Guid EquipmentTypeId { get; set; }
    public string OperationalState { get; set; } = null!;
}

/// <summary>Equipment / chambers，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Chamber
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid EquipmentId { get; set; }
}

/// <summary>Equipment / equipment_events，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class EquipmentEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid? MachineConnectionId { get; set; }
    public string EventType { get; set; } = null!;
    public string? StateFrom { get; set; }
    public string? StateTo { get; set; }
    public string? Payload { get; set; }
    public DateTime EventTimestamp { get; set; }
    public DateTime ReceivedAt { get; set; }
    public Guid CorrelationId { get; set; }
    public bool IsSimulated { get; set; }
}

/// <summary>Equipment / equipment_states，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class EquipmentState
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid EquipmentId { get; set; }
    public string State { get; set; } = null!;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Source { get; set; } = null!;
    public string? Metadata { get; set; }
}

/// <summary>Equipment / downtimes，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Downtime
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid? LineId { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public Guid? ShiftId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Notes { get; set; }
    public Guid ReportedById { get; set; }
    public bool NeedsReason { get; set; }
    public DateTime? ClassifiedAt { get; set; }
    public Guid? ClassifiedById { get; set; }
}

/// <summary>Equipment / tools，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Tool
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? EquipmentTypeId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? NextServiceAt { get; set; }
}

/// <summary>Equipment / maintenance_schedules，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaintenanceSchedule
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ToolId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string EventType { get; set; } = null!;
    public Guid? AssignedToId { get; set; }
    public string Frequency { get; set; } = null!;
    public int IntervalValue { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public DateTime NextDueAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Equipment / maintenance_events，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaintenanceEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Title { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid? ToolId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid? AssignedToId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

/// <summary>Equipment / equipment_qualifications，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class EquipmentQualification
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid OperationId { get; set; }
    public Guid? RecipeVersionId { get; set; }
    public string QualificationStatus { get; set; } = null!;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public Guid QualifiedById { get; set; }
}

/// <summary>Equipment / worker_qualifications，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class WorkerQualification
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid WorkerId { get; set; }
    public Guid OperationId { get; set; }
    public Guid? RecipeVersionId { get; set; }
    public string QualificationStatus { get; set; } = null!;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public Guid QualifiedById { get; set; }
}

/// <summary>Equipment / reticles，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Reticle
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string Revision { get; set; } = null!;
    public string LayerCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public long UsageCount { get; set; }
}

/// <summary>Equipment / reticle_usages，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ReticleUsage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid ReticleId { get; set; }
    public Guid ProcessRunId { get; set; }
    public DateTime LoadedAt { get; set; }
    public DateTime? UnloadedAt { get; set; }
}

/// <summary>Equipment / material_mounts，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaterialMount
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid MaterialLotId { get; set; }
    public Guid EquipmentId { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime MountedAt { get; set; }
    public DateTime? UnmountedAt { get; set; }
    public DateTime? UsableUntil { get; set; }
    public decimal UsageQuantity { get; set; }
    public string Unit { get; set; } = null!;
}
