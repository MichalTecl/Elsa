IF OBJECT_ID('dbo.vwOrderVocMoc', 'V') IS NOT NULL
    DROP VIEW dbo.vwOrderVocMoc;
GO

CREATE VIEW dbo.vwOrderVocMoc AS
SELECT 
    c.Id AS CustomerId,
    po.Id AS PurchaseOrderId,
    po.OrderNumber,
    po.PurchaseDate,
    itemsPrice.price AS MOC,
    po.Price - priceElements.ttl AS VOC
FROM PurchaseOrder po
JOIN Customer c ON c.ErpUid = po.CustomerErpUid
JOIN (
    SELECT 
        oi.PurchaseOrderId, 
        SUM(oi.TaxedPrice / (1 + (oi.TaxPercent / 100))) AS price
    FROM OrderItem oi
    GROUP BY oi.PurchaseOrderId
) itemsPrice ON po.Id = itemsPrice.PurchaseOrderId
JOIN (
    SELECT 
        pe.PurchaseOrderId, 
        SUM(pe.Price) AS ttl
    FROM vwPriceElements pe
    WHERE pe.TypeName <> 'percent_discount'
    GROUP BY pe.PurchaseOrderId
) priceElements ON priceElements.PurchaseOrderId = po.Id;
GO
