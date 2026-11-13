namespace MESagent.Domain.Entities;

public sealed class WaferRelease
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid WaferId { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid InitialLotId { get; set; }
    public int SnapshotVersion { get; set; }
    public Guid RouteVersionId { get; set; }
    public DateTime ReleasedAt { get; set; }
    public Guid ReleasedById { get; set; }
    public Guid CommandId { get; set; }
}

public sealed class WaferCompletion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid WaferId { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid LotIdAtCompletion { get; set; }
    public Guid ProcessRunId { get; set; }
    public Guid RouteVersionId { get; set; }
    public Guid RouteStepId { get; set; }
    public DateTime CompletedAt { get; set; }
    public Guid CompletedById { get; set; }
    public Guid CommandId { get; set; }
}

public sealed class StepExecutionInheritance
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid GenealogyEventId { get; set; }
    public Guid SourceStepExecutionId { get; set; }
    public Guid TargetStepExecutionId { get; set; }
}
