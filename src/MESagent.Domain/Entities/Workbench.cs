namespace MESagent.Domain.Entities;

public sealed class EquipmentIdealRate
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid FabId { get; set; }
    public Guid EquipmentId { get; set; }
    public DateOnly EffectiveOn { get; set; }
    public decimal IdealSeconds { get; set; }
}

public sealed class ProductionScheduleBlock
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid FabId { get; set; }
    public Guid StepExecutionId { get; set; }
    public Guid EquipmentId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public uint RowVersion { get; set; }
}

public sealed class WorkbenchSubscription
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid FabId { get; set; }
    public Guid UserId { get; set; }
    public string Kind { get; set; } = null!;
    public bool Enabled { get; set; }
    public DateTime EnabledAt { get; set; }
    public int? EscalationMinutes { get; set; }
    public Guid? EscalateToId { get; set; }
}

public sealed class WorkbenchAlertReceipt
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid FabId { get; set; }
    public Guid UserId { get; set; }
    public string Kind { get; set; } = null!;
    public Guid SourceId { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? SilencedUntil { get; set; }
}
