namespace MESagent.Application;

public sealed record RegisterWaferMaterial(string Code, string Name);
public sealed record RegisterWaferMaterialSource(Guid MaterialId, string Code, string Name);
public sealed record RegisterWaferMaterialLot(
    Guid SourceId,
    string LotNumber,
    string SupplierLotNo,
    DateOnly? ExpiryDate = null);
