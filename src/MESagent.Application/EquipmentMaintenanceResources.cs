namespace MESagent.Application;

public interface IEquipmentMaintenanceResourcesBackend
{
    Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest request);
}

public sealed record UpdateEquipmentReference(string Name, bool IsActive, string? Description = null);

public sealed record EditEquipment(
    string Name, bool IsActive, Guid AreaId, Guid EquipmentGroupId, Guid EquipmentTypeId,
    Guid? LineId = null, string? Description = null);

public sealed record ChangeEquipmentState(
    string State, string Reason, Guid? ReasonCodeId = null, Guid? ShiftId = null);

public sealed record SimulateEquipmentEvent(
    string Source, string SourceEventId, DateTime OccurredAt, string Kind, string State, string Reason,
    Guid? ReasonCodeId = null, Guid? ShiftId = null);

public sealed record ClassifyDowntime(Guid ReasonCodeId, string Notes, Guid? ShiftId = null);

public sealed record RegisterDowntime(Guid EquipmentStateId, Guid? ReasonCodeId = null, Guid? ShiftId = null,
    string? Notes = null);

public sealed record RegisterEquipmentTool(string Code, string Name, Guid? EquipmentId = null,
    Guid? EquipmentTypeId = null, string? Description = null);

public sealed record EditEquipmentTool(string Name, bool IsActive, string Status, string? Description = null,
    Guid? EquipmentId = null);

public sealed record CreateMaintenancePlan(string Name, string EventType, string Frequency, int IntervalValue,
    DateTime NextDueAt, Guid? EquipmentId = null, Guid? ToolId = null, Guid? AssignedToId = null);

public sealed record CreateMaintenanceWork(string Title, string EventType, DateTime ScheduledAt,
    Guid? EquipmentId = null, Guid? ToolId = null, Guid? ScheduleId = null, Guid? AssignedToId = null);

public sealed record DeferMaintenance(DateTime Until, string Reason);

public sealed record CreateEquipmentQualification(Guid EquipmentId, Guid OperationId, DateTime ValidFrom,
    DateTime? ValidTo = null, Guid? RecipeVersionId = null);

public sealed record CreateWorkerQualification(Guid WorkerId, Guid OperationId, DateTime ValidFrom,
    DateTime? ValidTo = null, Guid? RecipeVersionId = null);

public sealed record SaveEquipmentOperationPolicy(Guid OperationId, bool RequireEquipmentQualification,
    bool RequireWorkerQualification, Guid[] RequiredMaterialIds, string? RequiredReticleLayer = null);

public sealed record RegisterReticle(string Code, string Name, string Revision, string LayerCode,
    long MaxUsageCount, DateTime? UsableUntil = null);

public sealed record EditReticle(string Name, string Status, bool IsActive);

public sealed record LoadReticle(Guid ReticleId);

public sealed record RegisterConsumable(string Code, string Name, string Unit);

public sealed record RegisterConsumableLot(Guid MaterialId, string LotNumber, DateTime? ExpiryAt = null);

public sealed record MountConsumable(Guid MaterialLotId, DateTime OpenedAt, DateTime UsableUntil);

public sealed record RegisterEquipmentShift(string Code, string Name, TimeOnly StartTime, TimeOnly EndTime);

public sealed record ToggleMaintenancePlan(bool IsActive, string Reason);

public sealed record RegisterEquipmentChamber(Guid EquipmentId, string Code, string Name);
