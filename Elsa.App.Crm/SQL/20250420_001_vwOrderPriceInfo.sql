IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERE name = 'vwOrderPriceInfo')
	DROP VIEW vwOrderPriceInfo;

GO

CREATE VIEW vwOrderPriceInfo
AS
select po.Id, (priceElms.PriceWithoutTax + items.PriceWithoutTax) PriceWithoutTax
from PurchaseOrder po

left join (SELECT pel.PurchaseOrderId, SUM(pel.Price) PriceWithoutTax 
             FROM vwPriceElements pel  
		  GROUP BY pel.PurchaseOrderId) priceElms ON (po.Id = priceElms.PurchaseOrderId)

left join (SELECT oi.PurchaseOrderId, SUM(oi.TaxedPrice / (1 + oi.TaxPercent / 100.0)) PriceWithoutTax
             FROM OrderItem oi
			WHERE oi.KitParentId Is NULL
		   GROUP BY oi.PurchaseOrderId) items ON (po.Id = items.PurchaseOrderId);