IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_PurchaseOrder_MissingPayments'
      AND object_id = OBJECT_ID('dbo.PurchaseOrder')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PurchaseOrder_MissingPayments
        ON dbo.PurchaseOrder (ProjectId)
        INCLUDE (Id)
        WHERE IsPayOnDelivery = 0
          AND OrderStatusId = 2;
END
GO
