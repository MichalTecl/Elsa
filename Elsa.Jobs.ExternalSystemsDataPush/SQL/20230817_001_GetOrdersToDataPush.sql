IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'GetOrdersToDataPush')
	DROP PROCEDURE GetOrdersToDataPush;

GO

CREATE PROCEDURE GetOrdersToDataPush (@projectId INT, @skip INT, @take INT)
AS
BEGIN
	WITH rnCustomers AS
	(
		SELECT c.*, ecpl.ExternalId RayNetCustomerId
		  FROM Customer c
		  JOIN EntityChangeProcessingLog ecpl ON (ecpl.EntityId = c.Id)
		 WHERE c.ProjectId = @projectId
		   AND ecpl.ProcessorName = 'Raynet_Customers_Push'
	)
	SELECT x.*,
		   itemsPrice.price OrderMOC,  
			x.OrderPrice - ISNULL(priceElements.ttl, 0) OrderVOC,
		   'Flox_' + oi.ErpProductId [ProductUid],
		   oi.PlacedName ProductName,
		   oi.Quantity ItemQuantity,
		   oi.TaxPercent ProductTaxPercent,
		   oi.TaxedPrice ItemTaxedPrice
	  FROM
	  (
			SELECT po.Id OrderId,	       
				   po.OrderNumber OrderNr,
				   po.OrderStatusId OrderStatusId,	
				   po.Price OrderPrice,
				   po.BuyDate,
				   MAX(COALESCE(c1.RayNetCustomerId, c2.RayNetCustomerId, c3.RayNetCustomerId)) CustomerRayNetId
			  FROM PurchaseOrder po
			  LEFT JOIN rnCustomers c1 ON (po.CustomerErpUid = c1.ErpUid)
			  LEFT JOIN rnCustomers c2 ON (LEN(ISNULL(po.VatId, '')) > 0 AND po.VatId = c2.VatId)
			  LEFT JOIN rnCustomers c3 ON (po.CustomerEmail = c3.Email)
			 WHERE po.OrderStatusId >= 5	   
			 GROUP BY po.Id, po.OrderNumber, po.OrderStatusId, po.Price, po.BuyDate
			 HAVING MAX(COALESCE(c1.RayNetCustomerId, c2.RayNetCustomerId, c3.RayNetCustomerId)) IS NOT NULL
			 ORDER BY po.Id
			 OFFSET @skip ROWS
			 FETCH NEXT @take ROWS ONLY
		) x   
	   JOIN OrderItem     oi ON (oi.PurchaseOrderId = x.OrderId)
	   JOIN (SELECT oi.PurchaseOrderId, SUM(oi.TaxedPrice / (1 + (oi.TaxPercent / 100))) price
			  FROM OrderItem oi
			 GROUP BY oi.PurchaseOrderId) itemsPrice ON (x.OrderId = itemsPrice.PurchaseOrderId)
	   LEFT JOIN (SELECT pe.PurchaseOrderId, SUM(pe.Price) ttl
				FROM vwPriceElements pe
				WHERE pe.TypeName NOT IN ('percent_discount')
				GROUP BY pe.PurchaseOrderId) priceElements ON (priceElements.PurchaseOrderId = x.OrderId)
END