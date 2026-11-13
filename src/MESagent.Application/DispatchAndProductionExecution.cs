namespace MESagent.Application;

public sealed record AssignProductionLot(Guid EquipmentId, Guid RecipeVersionId);

public sealed record TrackInProductionLot(Guid EquipmentId, string EquipmentEtag, Guid RecipeVersionId);

public sealed record RecordProductionOutput(
    Guid OutputId,
    string? Unit = null,
    string? ValueText = null,
    decimal? ValueNumber = null,
    bool? ValueBoolean = null,
    DateTime? ValueDate = null);

public sealed record RecordProductionWork(
    Guid[] ChecklistItemIds,
    RecordProductionOutput[] Outputs,
    Guid[] DocumentIds,
    bool Confirm = false,
    string? Notes = null);

public sealed record ProductionWaferResult(Guid WaferId, string Result, string? Notes = null);

public sealed record TrackOutProductionRun(ProductionWaferResult[] Wafers);

public sealed record CreateEquipmentReference(string Code, string Name);

public sealed record CreateExecutionEquipment(
    string Code,
    string Name,
    Guid AreaId,
    Guid EquipmentGroupId,
    Guid EquipmentTypeId,
    Guid? LineId = null);

public sealed record UpdateExecutionEquipment(string Name, bool IsActive);

public sealed record ChangeExecutionEquipmentState(string State, string Reason);

public sealed record ProductionProgressView(
    string Coverage,
    DateTime AsOf,
    string[] MissingDependencies,
    int PlannedQty,
    int? ReleasedQty,
    int? InProcessQty,
    int? BlockedQty,
    int? CompletedQty,
    int? ScrappedQty);

public interface IDispatchAndProductionExecutionBackend
{
    Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest request);
}
