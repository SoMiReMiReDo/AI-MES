namespace MESagent.Domain.Entities;

/// <summary>Production / work_orders，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class WorkOrder
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string OrderNo { get; set; } = null!;
    public Guid FabId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductRevisionId { get; set; }
    public int PlannedQty { get; set; }
    public int ProducedQty { get; set; }
    public string Status { get; set; } = null!;
    public int Priority { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? Description { get; set; }
    public DateTime? PlannedStartAt { get; set; }
    public DateTime? PlannedEndAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerOrderNo { get; set; }
    public string? ProcessSnapshot { get; set; }
    public int SnapshotVersion { get; set; }
}

/// <summary>Production / work_order_snapshots，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class WorkOrderSnapshot
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid WorkOrderId { get; set; }
    public int Version { get; set; }
    public string Snapshot { get; set; } = null!;
    public DateTime EffectiveFrom { get; set; }
    public Guid? EffectiveFromLotId { get; set; }
    public Guid? ChangeRequestId { get; set; }
    public Guid CreatedById { get; set; }
}

/// <summary>Production / work_order_change_requests，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class WorkOrderChangeRequest
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public Guid WorkOrderId { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Proposed { get; set; } = null!;
    public string PreviousValues { get; set; } = null!;
    public string? Impact { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public Guid? EffectiveFromLotId { get; set; }
    public Guid RequestedById { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedById { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? AppliedById { get; set; }
    public DateTime? AppliedAt { get; set; }
    public int? ResultingSnapshotVersion { get; set; }
}

/// <summary>Production / lots，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Lot
{
    public DateTime? ClosedAt { get; set; }
    public string? CloseReason { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid FabId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public string LotNumber { get; set; } = null!;
    public string? LegacyBatchNumber { get; set; }
    public DateTime LotAssignedAt { get; set; }
    public Guid ProductRevisionId { get; set; }
    public Guid RouteVersionId { get; set; }
    public Guid? CurrentRouteStepId { get; set; }
    public string LotType { get; set; } = null!;
    public int Priority { get; set; }
    public int TargetQty { get; set; }
    // Completion snapshot, updated by the terminal-operation command, never by station summation.
    public int ProducedQty { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ReleasedToRouteAt { get; set; }
    public Guid? ReleasedToRouteById { get; set; }
    public string? Notes { get; set; }
    public int? SnapshotVersion { get; set; }
}

/// <summary>Production / wafers，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Wafer
{
    // Nullable only for legacy rows; managed production always assigns both.
    public Guid? FabId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string WaferCode { get; set; } = null!;
    public Guid? OriginMaterialLotId { get; set; }
    public string? SupplierWaferCode { get; set; }
    public decimal WaferSizeMm { get; set; }
    public string LifecycleStatus { get; set; } = null!;
    public DateTime? ProducedAt { get; set; }
}

/// <summary>Production / genealogy_events，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class GenealogyEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string EventType { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public Guid PerformedById { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public Guid CorrelationId { get; set; }
}

/// <summary>Production / genealogy_details，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class GenealogyDetail
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid EventId { get; set; }
    public Guid WaferId { get; set; }
    public Guid? FromLotId { get; set; }
    public Guid ToLotId { get; set; }
}

/// <summary>Production / lot_memberships，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class LotMembership
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid WaferId { get; set; }
    public Guid LotId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public Guid ChangeEventId { get; set; }
}

/// <summary>Production / carriers，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Carrier
{
    public decimal? AllowedWaferSizeMm { get; set; }
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid FabId { get; set; }
    public string CarrierCode { get; set; } = null!;
    public string CarrierType { get; set; } = null!;
    public int SlotCapacity { get; set; }
    public string Status { get; set; } = null!;
    public Guid? CurrentLocationId { get; set; }
}

/// <summary>Production / carrier_slots，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class CarrierSlot
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid CarrierId { get; set; }
    public int SlotNo { get; set; }
}

/// <summary>Production / carrier_loads，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class CarrierLoad
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid CarrierSlotId { get; set; }
    public Guid WaferId { get; set; }
    public DateTime LoadedAt { get; set; }
    public DateTime? UnloadedAt { get; set; }
}

/// <summary>Production / carrier_movements，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class CarrierMovement
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid CarrierId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public DateTime MovedAt { get; set; }
    public Guid PerformedById { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Production / step_executions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class StepExecution
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid LotId { get; set; }
    public Guid RouteVersionId { get; set; }
    public Guid RouteStepId { get; set; }
    public int VisitNo { get; set; }
    public string Status { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Instruction { get; set; }
    public DateTime EnteredAt { get; set; }
    public DateTime? ExitedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedById { get; set; }
    public bool RequiresConfirmation { get; set; }
    public Guid? AssignedById { get; set; }
    public DateTime? AssignedAt { get; set; }
    public Guid? AssignedEquipmentId { get; set; }
    public Guid? AssignedRecipeVersionId { get; set; }
    public int PassedQty { get; set; }
    public int ScrapQty { get; set; }
}

/// <summary>Production / process_runs，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ProcessRun
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid StepExecutionId { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid? ChamberId { get; set; }
    public Guid RecipeVersionId { get; set; }
    public Guid? RecipeApplicabilityId { get; set; }
    public DateTime TrackInAt { get; set; }
    public DateTime? TrackOutAt { get; set; }
    public Guid TrackInById { get; set; }
    public Guid? TrackOutById { get; set; }
    public string Status { get; set; } = null!;
}

/// <summary>Production / process_run_wafers，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ProcessRunWafer
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid ProcessRunId { get; set; }
    public Guid WaferId { get; set; }
    public Guid? OperatorId { get; set; }
    public string? Result { get; set; }
    public string? Parameters { get; set; }
    public string? Notes { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>Production / track_events，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class TrackEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid ProcessRunId { get; set; }
    public string EventType { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public DateTime RecordedAt { get; set; }
    public Guid ActorId { get; set; }
    public string Source { get; set; } = null!;
    public Guid CorrelationId { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public string? Payload { get; set; }
}

/// <summary>Production / process_confirmations，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ProcessConfirmation
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid LotId { get; set; }
    public Guid? StepExecutionId { get; set; }
    public Guid ConfirmedById { get; set; }
    public DateTime ConfirmedAt { get; set; }
    public string ConfirmationType { get; set; } = null!;
    public string? Notes { get; set; }
    public string? Value { get; set; }
}

/// <summary>Production / output_values，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class OutputValue
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid StepExecutionId { get; set; }
    public Guid OutputId { get; set; }
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public bool? ValueBoolean { get; set; }
    public DateTime? ValueDate { get; set; }
    public Guid RecordedById { get; set; }
    public DateTime RecordedAt { get; set; }
}

/// <summary>Production / checklist_completions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ChecklistCompletion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid StepExecutionId { get; set; }
    public Guid ChecklistItemId { get; set; }
    public Guid CheckedById { get; set; }
    public DateTime CheckedAt { get; set; }
}

/// <summary>Production / step_documents，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class StepDocument
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid StepExecutionId { get; set; }
    public Guid? EngineeringDocumentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Reference { get; set; }
    public string? FilePath { get; set; }
    public bool IsMandatory { get; set; }
    public bool RequiresValidation { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public Guid? ValidatedById { get; set; }
}

/// <summary>Production / work_order_stops，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class WorkOrderStop
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid? LotId { get; set; }
    public string StopType { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public Guid? IssueId { get; set; }
    public Guid StoppedById { get; set; }
    public DateTime StoppedAt { get; set; }
    public Guid? ResumedById { get; set; }
    public DateTime? ResumedAt { get; set; }
    public string? ResumeNotes { get; set; }
    public string? Context { get; set; }
}

/// <summary>Production / process_batches，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ProcessBatch
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public Guid EquipmentId { get; set; }
    public string Status { get; set; } = null!;
}

/// <summary>Production / process_batch_members，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ProcessBatchMember
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid ProcessBatchId { get; set; }
    public Guid ProcessRunId { get; set; }
    public Guid LotId { get; set; }
    public Guid WaferId { get; set; }
    public int Position { get; set; }
}

/// <summary>Production / q_time_windows，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class QTimeWindow
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid QTimeRuleId { get; set; }
    public Guid LotId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime DeadlineAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime? ViolatedAt { get; set; }
}

/// <summary>Production / rework_records，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class ReworkRecord
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid LotId { get; set; }
    public Guid OriginalStepExecutionId { get; set; }
    public Guid ReworkRouteVersionId { get; set; }
    public Guid ReturnRouteStepId { get; set; }
    public Guid ReasonCodeId { get; set; }
    public Guid ApprovedById { get; set; }
    public DateTime ApprovedAt { get; set; }
}
