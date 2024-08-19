IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_PurchaseOrder_CustomerErpUid'
)
BEGIN
    CREATE INDEX IX_PurchaseOrder_CustomerErpUid ON PurchaseOrder(CustomerErpUid);
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Customer_ErpUid'
)
BEGIN
    CREATE INDEX IX_Customer_ErpUid ON Customer(ErpUid);
END
GO
