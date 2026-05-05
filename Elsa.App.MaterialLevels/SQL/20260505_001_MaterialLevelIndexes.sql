
/* =========================================================
   Material
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Material_Inventory_Group_Id'
      AND object_id = OBJECT_ID('dbo.Material')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Material_Inventory_Group_Id
    ON dbo.Material (InventoryId, MaterialLevelReportingGroup, Id)
    INCLUDE (Name, NominalUnitId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Material_Name_Id'
      AND object_id = OBJECT_ID('dbo.Material')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Material_Name_Id
    ON dbo.Material (Name, Id)
    INCLUDE (NominalUnitId, InventoryId, MaterialLevelReportingGroup);
END
GO


/* =========================================================
   MaterialBatch
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialBatch_Material_Close_FullSpend'
      AND object_id = OBJECT_ID('dbo.MaterialBatch')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialBatch_Material_Close_FullSpend
    ON dbo.MaterialBatch (MaterialId, CloseDt, FullSpendDt, Id)
    INCLUDE (BatchNumber, Created, UnitId, SupplierId, ProjectId, Volume);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialBatch_Material_Created'
      AND object_id = OBJECT_ID('dbo.MaterialBatch')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialBatch_Material_Created
    ON dbo.MaterialBatch (MaterialId, Created)
    INCLUDE (Id, BatchNumber, SupplierId, UnitId, ProjectId, Volume, CloseDt, FullSpendDt);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialBatch_Material_Created_Supplier'
      AND object_id = OBJECT_ID('dbo.MaterialBatch')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialBatch_Material_Created_Supplier
    ON dbo.MaterialBatch (MaterialId, Created DESC)
    INCLUDE (SupplierId, Id);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialBatch_Supplier_Material_Created'
      AND object_id = OBJECT_ID('dbo.MaterialBatch')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialBatch_Supplier_Material_Created
    ON dbo.MaterialBatch (SupplierId, MaterialId, Created DESC)
    INCLUDE (Id);
END
GO


/* =========================================================
   MaterialOrderEvent
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialOrderEvent_Material_OrderDt_Id'
      AND object_id = OBJECT_ID('dbo.MaterialOrderEvent')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialOrderEvent_Material_OrderDt_Id
    ON dbo.MaterialOrderEvent (MaterialId, OrderDt DESC, Id DESC)
    INCLUDE (UserId, DeliveryDeadline, InsertDt);
END
GO


/* =========================================================
   MaterialStockEvent - větev vwBatchEvent
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialStockEvent_BatchId'
      AND object_id = OBJECT_ID('dbo.MaterialStockEvent')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialStockEvent_BatchId
    ON dbo.MaterialStockEvent (BatchId)
    INCLUDE (EventDt, Delta, UnitId, TypeId, ProjectId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialStockEvent_Batch_EventDt'
      AND object_id = OBJECT_ID('dbo.MaterialStockEvent')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialStockEvent_Batch_EventDt
    ON dbo.MaterialStockEvent (BatchId, EventDt)
    INCLUDE (Delta, UnitId, TypeId, ProjectId);
END
GO


/* =========================================================
   SaleEventAllocation - větev vwBatchEvent
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SaleEventAllocation_Batch_Allocation'
      AND object_id = OBJECT_ID('dbo.SaleEventAllocation')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SaleEventAllocation_Batch_Allocation
    ON dbo.SaleEventAllocation (BatchId, AllocationDt)
    INCLUDE (AllocatedQuantity, UnitId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_SaleEventAllocation_Batch_Return'
      AND object_id = OBJECT_ID('dbo.SaleEventAllocation')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SaleEventAllocation_Batch_Return
    ON dbo.SaleEventAllocation (BatchId, ReturnDt)
    INCLUDE (ReturnedQuantity, UnitId)
    WHERE ReturnDt IS NOT NULL;
END
GO


/* =========================================================
   MaterialBatchComposition - větev vwBatchEvent
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialBatchComposition_Component'
      AND object_id = OBJECT_ID('dbo.MaterialBatchComposition')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialBatchComposition_Component
    ON dbo.MaterialBatchComposition (ComponentId)
    INCLUDE (CompositionId, Volume, UnitId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialBatchComposition_Composition'
      AND object_id = OBJECT_ID('dbo.MaterialBatchComposition')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialBatchComposition_Composition
    ON dbo.MaterialBatchComposition (CompositionId)
    INCLUDE (ComponentId, Volume, UnitId);
END
GO


/* =========================================================
   OrderItemMaterialBatch - větev vwBatchEvent / eshop sale
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OrderItemMaterialBatch_MaterialBatch'
      AND object_id = OBJECT_ID('dbo.OrderItemMaterialBatch')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderItemMaterialBatch_MaterialBatch
    ON dbo.OrderItemMaterialBatch (MaterialBatchId)
    INCLUDE (OrderItemId, AssignmentDt, Quantity);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OrderItemMaterialBatch_OrderItem'
      AND object_id = OBJECT_ID('dbo.OrderItemMaterialBatch')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderItemMaterialBatch_OrderItem
    ON dbo.OrderItemMaterialBatch (OrderItemId)
    INCLUDE (MaterialBatchId, AssignmentDt, Quantity);
END
GO


/* =========================================================
   OrderItem - kit parent / purchase order joiny
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OrderItem_PurchaseOrderId'
      AND object_id = OBJECT_ID('dbo.OrderItem')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderItem_PurchaseOrderId
    ON dbo.OrderItem (PurchaseOrderId)
    INCLUDE (Id, KitParentId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OrderItem_KitParentId'
      AND object_id = OBJECT_ID('dbo.OrderItem')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OrderItem_KitParentId
    ON dbo.OrderItem (KitParentId)
    INCLUDE (Id, PurchaseOrderId);
END
GO


/* =========================================================
   UnitConversion / MaterialUnit - vwUnitConversion
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UnitConversion_Source_Target'
      AND object_id = OBJECT_ID('dbo.UnitConversion')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_UnitConversion_Source_Target
    ON dbo.UnitConversion (SourceUnitId, TargetUnitId)
    INCLUDE (Multiplier, ProjectId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UnitConversion_Target_Source'
      AND object_id = OBJECT_ID('dbo.UnitConversion')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_UnitConversion_Target_Source
    ON dbo.UnitConversion (TargetUnitId, SourceUnitId)
    INCLUDE (Multiplier, ProjectId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_MaterialUnit_Project_Id'
      AND object_id = OBJECT_ID('dbo.MaterialUnit')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_MaterialUnit_Project_Id
    ON dbo.MaterialUnit (ProjectId, Id);
END
GO


/* =========================================================
   Supplier - vwMaterialSupplier / finální SELECT
   ========================================================= */

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Supplier_DisableDt_Id'
      AND object_id = OBJECT_ID('dbo.Supplier')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Supplier_DisableDt_Id
    ON dbo.Supplier (DisableDt, Id)
    INCLUDE (Name, ContactEmail, ContactPhone);
END
GO