IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'GetOrderIdsByUsedBatch')
BEGIN
	DROP PROCEDURE GetOrderIdsByUsedBatch;
END

GO

CREATE PROCEDURE GetOrderIdsByUsedBatch (@projectId INT, @materialId INT, @batchNumber NVARCHAR(64), @skip INT, @take INT)
AS
BEGIN
	SELECT DISTINCT po.Id, ABS(po.OrderStatusId - 5) DUMMY
		  FROM PurchaseOrder po
		  INNER JOIN OrderItem oi ON (oi.PurchaseOrderId = po.Id)
		  LEFT JOIN OrderItem  ki ON (ki.KitParentId = oi.Id)
		  LEFT JOIN OrderItemMaterialBatch oimb ON (oimb.OrderItemId = ISNULL(ki.Id, oi.Id))
		  LEFT JOIN MaterialBatch mb ON (mb.Id = oimb.MaterialBatchId)
		WHERE mb.ProjectId = @projectId
		  AND mb.MaterialId = @materialId
		  AND mb.BatchNumber = @batchNumber
	 ORDER BY ABS(po.OrderStatusId - 5) DESC, po.Id ASC
	 OFFSET @skip ROWS
	 FETCH NEXT @take ROWS ONLY;
END


