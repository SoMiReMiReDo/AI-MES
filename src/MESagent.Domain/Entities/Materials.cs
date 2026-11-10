namespace MESagent.Domain.Entities;

/// <summary>Materials / companies，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Company
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string CompanyType { get; set; } = null!;
}

/// <summary>Materials / material_types，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaterialType
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

/// <summary>Materials / materials，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Material
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid MaterialTypeId { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public string TrackingType { get; set; } = null!;
    public Guid? SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? ExternalCode { get; set; }
    public string? ExternalSystem { get; set; }
}

/// <summary>Materials / warehouses，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class Warehouse
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
    public string Kind { get; set; } = null!;
    public string? ErpCode { get; set; }
}

/// <summary>Materials / material_sources，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaterialSource
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid MaterialId { get; set; }
    public Guid IntegrationConfigId { get; set; }
    public string ExternalCode { get; set; } = null!;
    public string? ExternalName { get; set; }
    public string? UnitMapping { get; set; }
    public decimal ConversionFactor { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public bool SyncEnabled { get; set; }
}

/// <summary>Materials / material_lots，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaterialLot
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string LotNumber { get; set; } = null!;
    public Guid MaterialId { get; set; }
    public Guid? SourceId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAvailable { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public DateTime ReceivedAt { get; set; }
    public DateOnly? ManufacturingDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Status { get; set; } = null!;
    public string? SupplierLotNo { get; set; }
    public string? SupplierReference { get; set; }
    public string? SourceContainerNo { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? InspectionId { get; set; }
    public Guid? IssueId { get; set; }
    public string? HoldReason { get; set; }
    public DateTime? HeldAt { get; set; }
    public Guid? HeldById { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
}

/// <summary>Materials / material_sublots，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaterialSublot
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid ParentLotId { get; set; }
    public string SublotNumber { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
}

/// <summary>Materials / bom_items，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class BomItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid RouteVersionId { get; set; }
    public Guid? RouteStepId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal QuantityPerUnit { get; set; }
    public string Per { get; set; } = null!;
    public decimal ScrapPercentage { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Materials / material_allocations，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaterialAllocation
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid? LotId { get; set; }
    public Guid? StepExecutionId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public decimal AllocatedQty { get; set; }
    public decimal ReturnedQty { get; set; }
    public decimal ExpectedQty { get; set; }
    public decimal ConsumedQty { get; set; }
    public decimal AdjustmentQty { get; set; }
    public decimal ScrapQty { get; set; }
    public string Status { get; set; } = null!;
    public Guid AllocatedById { get; set; }
    public DateTime AllocatedAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public Guid? ConsumptionWarehouseId { get; set; }
}

/// <summary>Materials / allocation_lot_picks，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class AllocationLotPick
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid MaterialAllocationId { get; set; }
    public Guid MaterialLotId { get; set; }
    public decimal PickedQty { get; set; }
    public string PickingStrategy { get; set; } = null!;
    public Guid? ConsumptionWarehouseId { get; set; }
}

/// <summary>Materials / material_consumptions，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class MaterialConsumption
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid StepExecutionId { get; set; }
    public Guid? ProcessRunId { get; set; }
    public Guid MaterialLotId { get; set; }
    public Guid? SublotId { get; set; }
    public decimal QuantityConsumed { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public DateTime ConsumedAt { get; set; }
    public Guid RecordedById { get; set; }
}

/// <summary>Materials / warehouse_stocks，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class WarehouseStock
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? MaterialLotId { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public DateTime? ErpSyncedAt { get; set; }
}

/// <summary>Materials / stock_movements，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class StockMovement
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid MaterialId { get; set; }
    public Guid? MaterialLotId { get; set; }
    public Guid WarehouseId { get; set; }
    public string MovementType { get; set; } = null!;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public decimal BalanceAfter { get; set; }
    public string SourceType { get; set; } = null!;
    public Guid SourceId { get; set; }
    public string Reason { get; set; } = null!;
    public Guid PerformedById { get; set; }
    public DateTime PerformedAt { get; set; }
}

/// <summary>Materials / stock_documents，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class StockDocument
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public string DocumentNo { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? LotId { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? PostedById { get; set; }
    public string? ErpReference { get; set; }
    public DateTime? ErpSyncedAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Materials / stock_document_lines，字段范围见 docs/development-process/04-fab-data-field-retention.md。</summary>
public sealed class StockDocumentLine
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Guid StockDocumentId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? MaterialLotId { get; set; }
    public string? LotNumber { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = null!;
    public string? Notes { get; set; }
}
