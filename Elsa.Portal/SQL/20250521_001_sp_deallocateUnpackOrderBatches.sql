IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'sp_deallocateUnpackOrderBatches')
    DROP PROCEDURE sp_deallocateUnpackOrderBatches;

GO

CREATE PROCEDURE [dbo].[sp_deallocateUnpackOrderBatches]
AS
BEGIN
	declare @fuckedOrderIems table (id int);
	insert into @fuckedOrderIems
	select oi.Id
	  from PurchaseOrder po
	  join OrderItem     oi on oi.PurchaseOrderId = po.Id
	  where po.OrderStatusId = 3
	    and not exists(SELECT TOP 1 1 FROM OrderProcessingBlocker b WHERE b.PurchaseOrderId = po.Id AND b.DisabledStageSymbol = 'Batch_assignment_change');

	insert into @fuckedOrderIems
	select Id from OrderItem where KitParentId in (select id from @fuckedOrderIems);

	SELECT DISTINCT b.MaterialBatchId
	  FROM OrderItemMaterialBatch b
	 WHERE OrderItemId IN (SELECT Id FROM @fuckedOrderIems);

	delete from OrderItemMaterialBatch where OrderItemId in (select id from @fuckedOrderIems);
END



