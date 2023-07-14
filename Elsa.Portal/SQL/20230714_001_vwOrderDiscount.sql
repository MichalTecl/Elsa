IF OBJECT_ID('vwOrderDiscount', 'V') IS NOT NULL
    DROP VIEW vwOrderDiscount;
GO

CREATE VIEW vwOrderDiscount AS
SELECT po.Id AS PurchaseOrderId,
       po.PercentDiscountText,
       po.PercentDiscountValue,
       ROUND(100 - (((po.PriceWithVat - priceElements.ttl) / itemsPrice.price) * 100), 0) AS DiscountPercent
FROM PurchaseOrder po
JOIN (
    SELECT oi.PurchaseOrderId, SUM(oi.TaxedPrice) AS price
    FROM OrderItem oi
    GROUP BY oi.PurchaseOrderId
) AS itemsPrice ON (po.Id = itemsPrice.PurchaseOrderId)
JOIN (
    SELECT pe.PurchaseOrderId, SUM(pe.TaxedPrice) AS ttl
    FROM vwPriceElements pe
    WHERE pe.TypeName NOT IN ('percent_discount')
    GROUP BY pe.PurchaseOrderId
) AS priceElements ON (priceElements.PurchaseOrderId = po.Id);
