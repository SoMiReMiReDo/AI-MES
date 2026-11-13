namespace MESagent.Application;

public sealed record RegisterWafer(
    string WaferCode,
    decimal WaferSizeMm,
    Guid? OriginMaterialLotId = null,
    string? SupplierWaferCode = null);

public sealed record CreateProductionLot(
    Guid WorkOrderId,
    RegisterWafer[] Wafers,
    string? LotNumber = null,
    int Priority = 50,
    string? Notes = null);

public sealed record UpdateProductionLot(int Priority, string? Notes = null);

public sealed record SplitProductionLot(Guid[] WaferIds, string Reason, string? NewLotNumber = null);

public sealed record MergeLotSource(Guid LotId, string Etag, Guid[] WaferIds);

public sealed record MergeProductionLots(MergeLotSource[] Sources, string Reason, string? NewLotNumber = null);

public sealed record CreateProductionCarrier(
    string CarrierCode,
    string CarrierType,
    int SlotCapacity,
    decimal AllowedWaferSizeMm,
    Guid? CurrentLocationId = null);

public sealed record UpdateProductionCarrier(string Status);

public sealed record MoveProductionCarrier(Guid ToLocationId, string Reason);

public sealed record ProductionVersion(Guid Id, string Etag);

public sealed record WaferSlotMove(Guid WaferId, Guid? FromSlotId, Guid? ToSlotId);

public sealed record ChangeCarrierLoads(
    ProductionVersion[] CarrierVersions,
    ProductionVersion[] WaferVersions,
    ProductionVersion[] LotVersions,
    WaferSlotMove[] Moves,
    string Reason);

public interface ILotWaferCarrierGenealogyBackend
{
    Task<OrganizationIdentityAccessControlResult> Execute(OrganizationIdentityAccessControlRequest request);
}

// Invoked by F5/F6 inside the authenticated caller's transaction, never directly from HTTP payloads.
public interface IProductionLifecycle
{
    Task<Guid> EnterWaitingStep(Guid lotId, Guid routeStepId, DateTime time, Guid callerCommandId);

    Task CompleteAtRouteEnd(Guid runId, Guid callerCommandId);

    Task CloseMembershipForScrap(Guid dispositionId, Guid callerCommandId);
}
