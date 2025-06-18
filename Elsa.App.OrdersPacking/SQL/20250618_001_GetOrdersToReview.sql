IF EXISTS(SELECT * FROM sys.procedures WHERE name = 'GetOrdersToReview')
	DROP PROCEDURE GetOrdersToReview;

GO

CREATE PROCEDURE GetOrdersToReview (@projectId INT,  @invalidKitNoteOrderIds IntTable READONLY)
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
	  LEFT JOIN Customer cu ON (cu.ErpUid = po.CustomerErpUid)
	  WHERE 1=1
		AND (po.OrderStatusId < 5 OR (DATEDIFF(day, po.PurchaseDate, GETDATE()) < 10))
		-- Not reviewed yet
		AND NOT EXISTS(SELECT TOP 1 1 FROM OrderReviewResult orr WHERE orr.OrderId = po.Id)

		AND (	  
		  /* Orders with customer note */
		  ((po.CustomerNote IS NOT NULL) AND (LEN(po.CustomerNote) > 0)) 			
		  OR
		  (po.Id IN (SELECT Id FROM @invalidKitNoteOrderIds)) -- If the kitnote is not valid
	  
		  OR	  
		  /* Orders outside of CZ */
		  (po.ShippingMethodName NOT LIKE N'Česká%')
	  
		  OR
		  /* Distributor orders */
		  (cu.IsDistributor = 1)
		)
	ORDER BY po.Id DESC
END
GO


