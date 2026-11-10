using MESagent.Domain.Entities;
using MESagent.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace MESagent.Infrastructure.Persistence;

internal static partial class FabModel
{
    private static void ConfigureMaterials(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(b =>
        {
            b.ToTable("companies", "materials", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.CompanyType).HasColumnName("company_type").HasMaxLength(32);
            b.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<MaterialType>(b =>
        {
            b.ToTable("material_types", "materials", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<Material>(b =>
        {
            b.ToTable("materials", "materials", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.MaterialTypeId).HasColumnName("material_type_id");
            b.HasOne<MaterialType>().WithMany().HasForeignKey(x => x.MaterialTypeId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(32);
            b.Property(x => x.TrackingType).HasColumnName("tracking_type").HasMaxLength(32);
            b.Property(x => x.SupplierId).HasColumnName("supplier_id");
            b.HasOne<Company>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.SupplierCode).HasColumnName("supplier_code").HasMaxLength(128);
            b.Property(x => x.ExternalCode).HasColumnName("external_code").HasMaxLength(128);
            b.Property(x => x.ExternalSystem).HasColumnName("external_system").HasMaxLength(64);
            b.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<Warehouse>(b =>
        {
            b.ToTable("warehouses", "materials", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);
            b.Property(x => x.Name).HasColumnName("name").HasMaxLength(200);
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.Property(x => x.FabId).HasColumnName("fab_id");
            b.HasOne<Fab>().WithMany().HasForeignKey(x => x.FabId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(32);
            b.Property(x => x.ErpCode).HasColumnName("erp_code").HasMaxLength(128);
            b.HasIndex(x => new { x.FabId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<MaterialSource>(b =>
        {
            b.ToTable("material_sources", "materials", t =>
            {
                t.HasCheckConstraint("ck_material_sources_1", "conversion_factor > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.MaterialId).HasColumnName("material_id");
            b.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.IntegrationConfigId).HasColumnName("integration_config_id");
            b.HasOne<IntegrationConfig>().WithMany().HasForeignKey(x => x.IntegrationConfigId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ExternalCode).HasColumnName("external_code").HasMaxLength(128);
            b.Property(x => x.ExternalName).HasColumnName("external_name").HasMaxLength(200);
            b.Property(x => x.UnitMapping).HasColumnName("unit_mapping").HasColumnType("jsonb");
            b.Property(x => x.ConversionFactor).HasColumnName("conversion_factor").HasPrecision(20, 6);
            b.Property(x => x.LastSyncedAt).HasColumnName("last_synced_at");
            b.Property(x => x.SyncEnabled).HasColumnName("sync_enabled");
            b.HasIndex(x => new { x.MaterialId, x.IntegrationConfigId, x.ExternalCode }).IsUnique();
        });
        modelBuilder.Entity<MaterialLot>(b =>
        {
            b.ToTable("material_lots", "materials", t =>
            {
                t.HasCheckConstraint("ck_material_lots_1", "quantity_received >= 0 AND quantity_available >= 0");
                t.HasCheckConstraint("ck_material_lots_2", "expiry_date IS NULL OR manufacturing_date IS NULL OR expiry_date >= manufacturing_date");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.LotNumber).HasColumnName("lot_number").HasMaxLength(64);
            b.Property(x => x.MaterialId).HasColumnName("material_id");
            b.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.SourceId).HasColumnName("source_id");
            b.HasOne<MaterialSource>().WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.QuantityReceived).HasColumnName("quantity_received").HasPrecision(20, 6);
            b.Property(x => x.QuantityAvailable).HasColumnName("quantity_available").HasPrecision(20, 6);
            b.Property(x => x.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(32);
            b.Property(x => x.ReceivedAt).HasColumnName("received_at");
            b.Property(x => x.ManufacturingDate).HasColumnName("manufacturing_date");
            b.Property(x => x.ExpiryDate).HasColumnName("expiry_date");
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.SupplierLotNo).HasColumnName("supplier_lot_no").HasMaxLength(128);
            b.Property(x => x.SupplierReference).HasColumnName("supplier_reference").HasMaxLength(128);
            b.Property(x => x.SourceContainerNo).HasColumnName("source_container_no").HasMaxLength(128);
            b.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.InspectionId).HasColumnName("inspection_id");
            b.HasOne<IncomingInspection>().WithMany().HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.IssueId).HasColumnName("issue_id");
            b.HasOne<Issue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.HoldReason).HasColumnName("hold_reason");
            b.Property(x => x.HeldAt).HasColumnName("held_at");
            b.Property(x => x.HeldById).HasColumnName("held_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.HeldById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ReleasedAt).HasColumnName("released_at");
            b.Property(x => x.ReleasedById).HasColumnName("released_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.ReleasedById).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.MaterialId, x.LotNumber }).IsUnique();
        });
        modelBuilder.Entity<MaterialSublot>(b =>
        {
            b.ToTable("material_sublots", "materials", t =>
            {
                t.HasCheckConstraint("ck_material_sublots_1", "quantity >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.ParentLotId).HasColumnName("parent_lot_id");
            b.HasOne<MaterialLot>().WithMany().HasForeignKey(x => x.ParentLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.SublotNumber).HasColumnName("sublot_number").HasMaxLength(64);
            b.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(20, 6);
            b.Property(x => x.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(32);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.Notes).HasColumnName("notes");
            b.HasIndex(x => new { x.ParentLotId, x.SublotNumber }).IsUnique();
        });
        modelBuilder.Entity<BomItem>(b =>
        {
            b.ToTable("bom_items", "materials", t =>
            {
                t.HasCheckConstraint("ck_bom_items_1", "quantity_per_unit >= 0 AND scrap_percentage BETWEEN 0 AND 100");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.RouteVersionId).HasColumnName("route_version_id");
            b.HasOne<RouteVersion>().WithMany().HasForeignKey(x => x.RouteVersionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.RouteStepId).HasColumnName("route_step_id");
            b.HasOne<RouteStep>().WithMany().HasForeignKey(x => x.RouteStepId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialId).HasColumnName("material_id");
            b.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.QuantityPerUnit).HasColumnName("quantity_per_unit").HasPrecision(20, 6);
            b.Property(x => x.Per).HasColumnName("per").HasMaxLength(32);
            b.Property(x => x.ScrapPercentage).HasColumnName("scrap_percentage").HasPrecision(20, 6);
            b.Property(x => x.Notes).HasColumnName("notes");
        });
        modelBuilder.Entity<MaterialAllocation>(b =>
        {
            b.ToTable("material_allocations", "materials", t =>
            {
                t.HasCheckConstraint("ck_material_allocations_1", "allocated_qty >= 0 AND returned_qty >= 0 AND expected_qty >= 0 AND consumed_qty >= 0 AND scrap_qty >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            b.HasOne<StepExecution>().WithMany().HasForeignKey(x => x.StepExecutionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialId).HasColumnName("material_id");
            b.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
            b.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.AllocatedQty).HasColumnName("allocated_qty").HasPrecision(20, 6);
            b.Property(x => x.ReturnedQty).HasColumnName("returned_qty").HasPrecision(20, 6);
            b.Property(x => x.ExpectedQty).HasColumnName("expected_qty").HasPrecision(20, 6);
            b.Property(x => x.ConsumedQty).HasColumnName("consumed_qty").HasPrecision(20, 6);
            b.Property(x => x.AdjustmentQty).HasColumnName("adjustment_qty").HasPrecision(20, 6);
            b.Property(x => x.ScrapQty).HasColumnName("scrap_qty").HasPrecision(20, 6);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.AllocatedById).HasColumnName("allocated_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.AllocatedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.AllocatedAt).HasColumnName("allocated_at");
            b.Property(x => x.ConsumedAt).HasColumnName("consumed_at");
            b.Property(x => x.ConsumptionWarehouseId).HasColumnName("consumption_warehouse_id");
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.ConsumptionWarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AllocationLotPick>(b =>
        {
            b.ToTable("allocation_lot_picks", "materials", t =>
            {
                t.HasCheckConstraint("ck_allocation_lot_picks_1", "picked_qty > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.MaterialAllocationId).HasColumnName("material_allocation_id");
            b.HasOne<MaterialAllocation>().WithMany().HasForeignKey(x => x.MaterialAllocationId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialLotId).HasColumnName("material_lot_id");
            b.HasOne<MaterialLot>().WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.PickedQty).HasColumnName("picked_qty").HasPrecision(20, 6);
            b.Property(x => x.PickingStrategy).HasColumnName("picking_strategy").HasMaxLength(32);
            b.Property(x => x.ConsumptionWarehouseId).HasColumnName("consumption_warehouse_id");
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.ConsumptionWarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<MaterialConsumption>(b =>
        {
            b.ToTable("material_consumptions", "materials", t =>
            {
                t.HasCheckConstraint("ck_material_consumptions_1", "quantity_consumed > 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.StepExecutionId).HasColumnName("step_execution_id");
            b.HasOne<StepExecution>().WithMany().HasForeignKey(x => x.StepExecutionId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProcessRunId).HasColumnName("process_run_id");
            b.HasOne<ProcessRun>().WithMany().HasForeignKey(x => x.ProcessRunId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialLotId).HasColumnName("material_lot_id");
            b.HasOne<MaterialLot>().WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.SublotId).HasColumnName("sublot_id");
            b.HasOne<MaterialSublot>().WithMany().HasForeignKey(x => x.SublotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.QuantityConsumed).HasColumnName("quantity_consumed").HasPrecision(20, 6);
            b.Property(x => x.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(32);
            b.Property(x => x.ConsumedAt).HasColumnName("consumed_at");
            b.Property(x => x.RecordedById).HasColumnName("recorded_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.RecordedById).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<WarehouseStock>(b =>
        {
            b.ToTable("warehouse_stocks", "materials", t =>
            {
                t.HasCheckConstraint("ck_warehouse_stocks_1", "quantity >= 0");
                t.HasCheckConstraint("ck_warehouse_stocks_owner", "num_nonnulls(material_id, product_id) = 1 AND (material_lot_id IS NULL OR material_id IS NOT NULL)");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialId).HasColumnName("material_id");
            b.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProductId).HasColumnName("product_id");
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialLotId).HasColumnName("material_lot_id");
            b.HasOne<MaterialLot>().WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(20, 6);
            b.Property(x => x.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(32);
            b.Property(x => x.ErpSyncedAt).HasColumnName("erp_synced_at");
        });
        modelBuilder.Entity<StockMovement>(b =>
        {
            b.ToTable("stock_movements", "materials", t =>
            {
                t.HasCheckConstraint("ck_stock_movements_1", "quantity <> 0 AND balance_after >= 0");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.MaterialId).HasColumnName("material_id");
            b.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialLotId).HasColumnName("material_lot_id");
            b.HasOne<MaterialLot>().WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MovementType).HasColumnName("movement_type").HasMaxLength(32);
            b.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(20, 6);
            b.Property(x => x.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(32);
            b.Property(x => x.BalanceAfter).HasColumnName("balance_after").HasPrecision(20, 6);
            b.Property(x => x.SourceType).HasColumnName("source_type").HasMaxLength(64);
            b.Property(x => x.SourceId).HasColumnName("source_id");
            b.Property(x => x.Reason).HasColumnName("reason");
            b.Property(x => x.PerformedById).HasColumnName("performed_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.PerformedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.PerformedAt).HasColumnName("performed_at");
        });
        modelBuilder.Entity<StockDocument>(b =>
        {
            b.ToTable("stock_documents", "materials", t =>
            {
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.DocumentNo).HasColumnName("document_no").HasMaxLength(64);
            b.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(32);
            b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32);
            b.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.WorkOrderId).HasColumnName("work_order_id");
            b.HasOne<WorkOrder>().WithMany().HasForeignKey(x => x.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotId).HasColumnName("lot_id");
            b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.PostedAt).HasColumnName("posted_at");
            b.Property(x => x.PostedById).HasColumnName("posted_by_id");
            b.HasOne<MesUser>().WithMany().HasForeignKey(x => x.PostedById).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ErpReference).HasColumnName("erp_reference").HasMaxLength(128);
            b.Property(x => x.ErpSyncedAt).HasColumnName("erp_synced_at");
            b.Property(x => x.Notes).HasColumnName("notes");
            b.HasIndex(x => x.DocumentNo).IsUnique();
        });
        modelBuilder.Entity<StockDocumentLine>(b =>
        {
            b.ToTable("stock_document_lines", "materials", t =>
            {
                t.HasCheckConstraint("ck_stock_document_lines_1", "quantity > 0");
                t.HasCheckConstraint("ck_stock_document_lines_2", "num_nonnulls(material_id, product_id) = 1");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
            b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.Property(x => x.StockDocumentId).HasColumnName("stock_document_id");
            b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.StockDocumentId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialId).HasColumnName("material_id");
            b.HasOne<Material>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.ProductId).HasColumnName("product_id");
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.MaterialLotId).HasColumnName("material_lot_id");
            b.HasOne<MaterialLot>().WithMany().HasForeignKey(x => x.MaterialLotId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.LotNumber).HasColumnName("lot_number").HasMaxLength(64);
            b.Property(x => x.Quantity).HasColumnName("quantity").HasPrecision(20, 6);
            b.Property(x => x.UnitOfMeasure).HasColumnName("unit_of_measure").HasMaxLength(32);
            b.Property(x => x.Notes).HasColumnName("notes");
        });
    }
}
