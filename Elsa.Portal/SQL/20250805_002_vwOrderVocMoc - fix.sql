IF OBJECT_ID('dbo.vwOrderVocMoc', 'V') IS NOT NULL
    DROP VIEW dbo.vwOrderVocMoc;
GO

CREATE VIEW dbo.vwOrderVocMoc AS
SELECT 
    c.Id AS CustomerId,
    po.Id AS PurchaseOrderId,
    po.OrderNumber,
    po.PurchaseDate,
    ROUND(itemsPrice.price, 2) AS MOC,
    ROUND(po.Price 
         - (po.TaxedShippingCost / (1 + po.ShippingTaxPercent / 100)) 
         - (po.TaxedPaymentCost / (1 + po.PaymentTaxPercent / 100)), 2)   AS VOC
FROM PurchaseOrder po
JOIN Customer c ON c.ErpUid = po.CustomerErpUid
JOIN (
    SELECT 
        oi.PurchaseOrderId, 
        SUM(oi.TaxedPrice / (1 + (oi.TaxPercent / 100))) AS price
    FROM OrderItem oi
    GROUP BY oi.PurchaseOrderId
) itemsPrice ON po.Id = itemsPrice.PurchaseOrderId


GO
