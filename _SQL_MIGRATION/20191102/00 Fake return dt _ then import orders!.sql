update purchaseorder 
set ReturnDt = InsertDt + 7
where Id in (
	select distinct po.Id
	  from PurchaseOrder po
	  join OrderItem oi on (oi.PurchaseOrderId = po.Id)
	  left join OrderItem ki on (ki.KitParentId = oi.Id)
	  join OrderItemMaterialBatch oimb ON (ISNULL(ki.Id, oi.Id) = oimb.OrderItemId)
	  join OrderStatus os on po.OrderStatusId = os.id
	where po.OrderStatusId in (6, 7, 8)
	  and po.ReturnDt is null
);


  
