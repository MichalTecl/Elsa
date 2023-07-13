-- Drop the view if it already exists
IF OBJECT_ID('vwPriceElements', 'V') IS NOT NULL
    DROP VIEW vwPriceElements
GO

-- Create the view
CREATE VIEW vwPriceElements AS
SELECT ope.PurchaseOrderId, 
       ope.Id AS PriceElementId,
       ope.TypeName, 
       ope.Title, 
       ISNULL(ope.Price, 0) AS Price, 
       ISNULL(ope.Tax, 0) AS TaxPercent, 
       ISNULL((ISNULL(ope.Tax, 0) / 100 + 1) * ope.Price, 0) AS TaxedPrice 
FROM OrderPriceElement ope;
