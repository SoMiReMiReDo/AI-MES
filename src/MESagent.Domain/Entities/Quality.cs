namespace MESagent.Domain.Entities;

/// <summary>Quality / reason_codes，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ReasonCode
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string Category { get; set; } = null!;
}

/// <summary>Quality / issue_types，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class IssueType
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string Severity { get; set; } = null!;
    public bool IsBlocking { get; set; }
}

/// <summary>Quality / issues，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Issue
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? StepExecutionId { get; set; }
    public Guid? LotId { get; set; }
    public Guid? WaferId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid IssueTypeId { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid ReportedById { get; set; }
    public Guid? AssignedToId { get; set; }
    public DateTime ReportedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? Disposition { get; set; }
    public int? NonConformingQty { get; set; }
    public string? RootCause { get; set; }
    public string? ContainmentAction { get; set; }
    public Guid? DispositionById { get; set; }
    public DateTime? DispositionAt { get; set; }
}

/// <summary>Quality / issue_actions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class IssueAction
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid IssueId { get; set; }
    public string ActionType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Guid AssignedToId { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedById { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedById { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Quality / hold_records，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class HoldRecord
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid LotId { get; set; }
    public Guid ReasonCodeId { get; set; }
    public Guid? IssueId { get; set; }
    public string HoldScope { get; set; } = null!;
    public DateTime HeldAt { get; set; }
    public Guid HeldById { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
    public string? ReleaseReason { get; set; }
}

/// <summary>Quality / material_holds，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaterialHold
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid MaterialLotId { get; set; }
    public Guid ReasonCodeId { get; set; }
    public Guid? IssueId { get; set; }
    public DateTime HeldAt { get; set; }
    public Guid HeldById { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
    public string? ReleaseReason { get; set; }
}

/// <summary>Quality / lot_releases，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class LotRelease
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid LotId { get; set; }
    public string ReleaseType { get; set; } = null!;
    public DateTime ReleasedAt { get; set; }
    public Guid ReleasedById { get; set; }
    public string Reason { get; set; } = null!;
    public Guid? IssueId { get; set; }
}

/// <summary>Quality / inspection_plans，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class InspectionPlan
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Name { get; set; } = null!;
    public Guid? MaterialId { get; set; }
    public Guid? MaterialTypeId { get; set; }
    public string Criteria { get; set; } = null!;
    public int Version { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? RootId { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Quality / incoming_inspections，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class IncomingInspection
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid InspectionPlanId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid? MaterialLotId { get; set; }
    public string LotNumber { get; set; } = null!;
    public string? SupplierLotRef { get; set; }
    public decimal QuantityReceived { get; set; }
    public Guid InspectorId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = null!;
    public string? Disposition { get; set; }
    public string? DispositionNotes { get; set; }
    public Guid? DispositionById { get; set; }
    public DateTime? DispositionAt { get; set; }
    public int PlanVersion { get; set; }
    public Guid? IssueId { get; set; }
}

/// <summary>Quality / inspection_results，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class InspectionResult
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid InspectionId { get; set; }
    public string CriterionName { get; set; } = null!;
    public string CriterionType { get; set; } = null!;
    public bool Required { get; set; }
    public string? Unit { get; set; }
    public decimal? SpecMin { get; set; }
    public decimal? SpecMax { get; set; }
    public decimal? ValueNumeric { get; set; }
    public bool? ValueBoolean { get; set; }
    public string? ValueText { get; set; }
    public bool? IsPassed { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Quality / quality_check_templates，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class QualityCheckTemplate
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid RouteVersionId { get; set; }
    public Guid? RouteStepId { get; set; }
    public string Name { get; set; } = null!;
    public int Version { get; set; }
    public int MinChecksPerBatch { get; set; }
    public int SamplesPerCheck { get; set; }
    public string? Parameters { get; set; }
}

/// <summary>Quality / specification_versions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class SpecificationVersion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string ParameterCode { get; set; } = null!;
    public int Version { get; set; }
    public string ValueType { get; set; } = null!;
    public string? Unit { get; set; }
    public decimal? SpecMin { get; set; }
    public decimal? SpecMax { get; set; }
    public string LifecycleStatus { get; set; } = null!;
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
}

/// <summary>Quality / quality_checks，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class QualityCheck
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid LotId { get; set; }
    public Guid? ProcessRunId { get; set; }
    public Guid QualityCheckTemplateId { get; set; }
    public Guid CheckedById { get; set; }
    public DateTime CheckedAt { get; set; }
    public int ProductionQuantity { get; set; }
    public bool? AllPassed { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Quality / measurements，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Measurement
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid QualityCheckId { get; set; }
    public Guid? ProcessRunId { get; set; }
    public Guid LotId { get; set; }
    public Guid? WaferId { get; set; }
    public int SampleNumber { get; set; }
    public string? SamplePoint { get; set; }
    public Guid SpecificationVersionId { get; set; }
    public string ParameterCode { get; set; } = null!;
    public string ParameterType { get; set; } = null!;
    public string? Unit { get; set; }
    public decimal? SpecMin { get; set; }
    public decimal? SpecMax { get; set; }
    public decimal? ValueNumeric { get; set; }
    public bool? ValueBoolean { get; set; }
    public string? ValueText { get; set; }
    public bool IsPassed { get; set; }
    public DateTime MeasuredAt { get; set; }
    public Guid? MeasurementEquipmentId { get; set; }
    public Guid RecordedById { get; set; }
}

/// <summary>Quality / quality_triggers，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class QualityTrigger
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Name { get; set; } = null!;
    public string TriggerType { get; set; } = null!;
    public Guid QualityCheckTemplateId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? ProductId { get; set; }
    public int? ThresholdN { get; set; }
    public decimal? DowntimeMinMinutes { get; set; }
    public bool IsBlocking { get; set; }
    public bool IsActive { get; set; } = true;
    public int Version { get; set; }
}

/// <summary>Quality / quality_tasks，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class QualityTask
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid QualityTriggerId { get; set; }
    public string Status { get; set; } = null!;
    public Guid? LotId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string DueReason { get; set; } = null!;
    public Guid? QualityCheckId { get; set; }
    public Guid? IssueId { get; set; }
    public DateTime FiredAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedById { get; set; }
}

/// <summary>Quality / scrap_entries，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ScrapEntry
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? StepExecutionId { get; set; }
    public Guid ReasonCodeId { get; set; }
    public int Quantity { get; set; }
    public Guid? ShiftId { get; set; }
    public string? Notes { get; set; }
    public Guid ReportedById { get; set; }
    public DateTime ReportedAt { get; set; }
}

/// <summary>Quality / wafer_dispositions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class WaferDisposition
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid WaferId { get; set; }
    public Guid LotIdAtEvent { get; set; }
    public Guid? ProcessRunId { get; set; }
    public Guid? ScrapEntryId { get; set; }
    public string DispositionType { get; set; } = null!;
    public Guid ReasonCodeId { get; set; }
    public Guid? IssueId { get; set; }
    public Guid ApprovedById { get; set; }
    public Guid PerformedById { get; set; }
    public DateTime OccurredAt { get; set; }
}
