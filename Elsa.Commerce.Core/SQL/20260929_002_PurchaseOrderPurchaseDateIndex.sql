IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PurchaseOrder')
      AND name = N'IX_PurchaseOrder_PurchaseDate'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PurchaseOrder_PurchaseDate
        ON dbo.PurchaseOrder (PurchaseDate);
END;
GO
