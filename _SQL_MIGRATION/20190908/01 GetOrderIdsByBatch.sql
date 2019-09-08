
IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'GetOrderIdsByUsedBatch')
BEGIN
	DROP PROCEDURE GetOrderIdsByUsedBatch;
END

GO

CREATE PROCEDURE GetOrderIdsByUsedBatch (@projectId INT, @materialId INT, @batchNumber NVARCHAR(64), @skip INT, @take INT)
AS
BEGIN
	SELECT DISTINCT po.Id, ABS(po.OrderStatusId - 5) PRIO, OrderNumber PONR, usages.used
		  FROM PurchaseOrder po
		  INNER JOIN OrderItem oi ON (po.Id = dbo.GetOrderItemOrderId(oi.Id))		  	  
		  INNER JOIN (SELECT oimb.OrderItemId, SUM(oimb.Quantity) as used
		                FROM OrderItemMaterialBatch oimb
						JOIN MaterialBatch mb ON (mb.Id = oimb.MaterialBatchId)
					  WHERE mb.MaterialId = @materialId
					    AND mb.BatchNumber = @batchNumber						
					  GROUP BY oimb.OrderItemId
					  HAVING SUM(oimb.Quantity) > 0) as usages ON (usages.OrderItemId = oi.Id)


		WHERE po.ProjectId = @projectId
	 ORDER BY PRIO DESC, PONR ASC
	 OFFSET @skip ROWS
	 FETCH NEXT @take ROWS ONLY;
END


