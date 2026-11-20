namespace MESagent.Application;

public static class WorkbenchAccess
{
    public const string Lots = "production.lots.read";
    public const string Execution = "execution.read";
    public const string Quality = "quality.read";
    public const string Equipment = "equipment.read";
    public const string Schedule = "workbench.schedule.manage";
    public const string ConfigureRate = "workbench.oee.configure";

    public static readonly string[] Sources = [Lots, Execution, Quality, Equipment];
}

public sealed record WorkbenchOption(Guid Id, string Code, string Name);

public sealed record WorkbenchOptions(
    WorkbenchOption[] Areas, WorkbenchOption[] Operations, WorkbenchOption[] Groups,
    WorkbenchOption[] Products, WorkbenchOption[] Equipment, WorkbenchOption[] Shifts, string DefaultScheduleView);

public sealed record WorkbenchOverview(
    DateTime AsOf, int? WipLots, int? WipWafers, int? Waiting, int? Running, int? Held,
    int? Equipment, int? EquipmentDown, int? CompletedToday);

public sealed record WorkbenchRecord
{
    public required Guid Id { get; init; }
    public required string Kind { get; init; }
    public required string Label { get; init; }
    public required string Status { get; init; }
    public required DateTime OccurredAt { get; init; }
    public Guid? LotId { get; init; }
    public Guid? EquipmentId { get; init; }
    public Guid? RunId { get; init; }
    public double? Minutes { get; init; }
    public required int Quantity { get; init; }
    public string? Detail { get; init; }
}

public sealed record WorkbenchHistoryDay(
    DateOnly Date, int? Passed, int? Completed, int? Failed, int? Scrap, int? FailedChecks,
    double? WaitMinutes, double? CycleMinutes);

public sealed record WorkbenchShiftMetric(
    Guid EquipmentId, string EquipmentCode, string? Shift, DateTime? StartsAt, DateTime? EndsAt,
    double? PlannedMinutes, double? RunMinutes, double? DowntimeMinutes, decimal? IdealSeconds,
    double? Availability, double? Performance, double? Quality, double? Oee,
    double? NonRunningMinutes, double? PerformanceLossMinutes, double? FailedWafers, string? Missing);

public sealed record WorkbenchBottleneck(Guid OperationId, string Name, int Waiting, DateTime Oldest);

public sealed record WorkbenchShifts(
    PageView<WorkbenchShiftMetric> Metrics, WorkbenchBottleneck[] Bottlenecks);

public sealed record AddWorkbenchRate(Guid EquipmentId, DateOnly EffectiveOn, decimal IdealSeconds);

public sealed record WorkbenchPlanInput(Guid StepExecutionId, Guid EquipmentId, DateTime StartsAt, DateTime EndsAt);

public sealed record WorkbenchAutoPlanInput(DateOnly From, DateOnly To, int MaxLots);

public sealed record WorkbenchPlan(
    Guid Id, Guid StepExecutionId, Guid LotId, string LotNumber, string Step, Guid EquipmentId,
    DateTime StartsAt, DateTime EndsAt, string Status, string Etag);

public sealed record WorkbenchCapacity(
    Guid EquipmentId, string Label, DateTime StartsAt, DateTime EndsAt, DateTime? BlockedAt);

public sealed record WorkbenchQueueItem(
    Guid StepExecutionId, Guid LotId, string LotNumber, string Step, int Priority, DateTime EnteredAt);

public sealed record WorkbenchPlanChange(
    DateTime OccurredAt, string Action, string? Actor, WorkbenchPlan? Before, WorkbenchPlan? After);

public sealed record WorkbenchScheduleDay(DateOnly Date, DateTime StartsAt, DateTime EndsAt);

public sealed record WorkbenchSchedule(
    DateTime StartsAt, DateTime EndsAt, WorkbenchOption[] Equipment, WorkbenchPlan[] Plans, WorkbenchCapacity[] Capacity,
    PageView<WorkbenchQueueItem> Queue, WorkbenchScheduleDay[] Days);

public sealed record WorkbenchUnplanned(Guid LotId, string LotNumber, string Reason);

public sealed record WorkbenchAutoResult(WorkbenchPlan[] Created, WorkbenchUnplanned[] Unplanned);

public sealed record WorkbenchSubscriptionInput(bool Enabled, int? EscalationMinutes, Guid? EscalateToId);

public sealed record WorkbenchSubscriptionView(
    string Kind, bool Enabled, int? EscalationMinutes, Guid? EscalateToId, WorkbenchOption[] Recipients);

public sealed record WorkbenchAlertInput(string Action, DateTime? Until);

public sealed record WorkbenchAlert(
    WorkbenchRecord Source, DateTime? AcknowledgedAt, DateTime? SilencedUntil, bool Escalated);

public sealed record WorkbenchAlerts(PageView<WorkbenchAlert> Records, int Unread);
