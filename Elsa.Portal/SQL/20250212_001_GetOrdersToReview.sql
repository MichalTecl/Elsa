IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'GetOrdersToReview')
	DROP PROCEDURE GetOrdersToReview;

GO

CREATE PROCEDURE GetOrdersToReview (@projectId INT)
AS
BEGIN
	SELECT TOP 10
		po.Id OrderId,
		po.OrderNumber OrderNr,
		po.CustomerName CustomerName,
		po.CustomerEmail CustomerEmail,
		FORMAT(po.PurchaseDate, 'dd. MM. yyyy HH:mm') OrderDt,
		po.ShippingMethodName Shipping,
		po.ErpStatusName [Status],
		po.CustomerNote CustomerNote,
		LTRIM(STR(po.PriceWithVat)) + ' CZK' Price
	  FROM PurchaseOrder po
	  JOIN Customer cu ON (cu.ErpUid = po.CustomerErpUid)
	  WHERE po.ProjectId = @projectId
		AND po.OrderStatusId < 5
		-- Not reviewed yet
		AND NOT EXISTS(SELECT TOP 1 1 FROM OrderReviewResult orr WHERE orr.OrderId = po.Id)

		AND (
	  
		  /* Orders with customer note */
		  (     (po.CustomerNote IS NOT NULL) 
			AND (LEN(po.CustomerNote) > 0) 
			AND NOT (
				PATINDEX('%------%', TRIM(REPLACE(REPLACE(REPLACE(REPLACE(po.CustomerNote, CHAR(13), ''), CHAR(10), ''), CHAR(9), ''), CHAR(160), ''))) = 1 
				AND 
				PATINDEX('%------', TRIM(REPLACE(REPLACE(REPLACE(REPLACE(po.CustomerNote, CHAR(13), ''), CHAR(10), ''), CHAR(9), ''), CHAR(160), ''))) = (LEN(TRIM(REPLACE(REPLACE(REPLACE(REPLACE(po.CustomerNote, CHAR(13), ''), CHAR(10), ''), CHAR(9), ''), CHAR(160), ''))) - 5)
			))
	  
		  OR	  
		  /* Orders outside of CZ */
		  (po.ShippingMethodName NOT LIKE N'Česká%')
	  
		  OR
		  /* Distributor orders */
		  (cu.IsDistributor = 1)
		)
	ORDER BY po.Id DESC
END
	