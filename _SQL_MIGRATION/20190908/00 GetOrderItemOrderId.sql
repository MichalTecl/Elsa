IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GetOrderItemOrderId]'))
BEGIN
	DROP FUNCTION dbo.GetOrderItemOrderId;
END

GO

CREATE FUNCTION dbo.GetOrderItemOrderId (@orderItemId INT) RETURNS INT
AS
BEGIN	
	RETURN (SELECT TOP 1 ISNULL(oi.PurchaseOrderId, kp.PurchaseOrderId)
	          FROM OrderItem oi
			  LEFT JOIN OrderItem kp ON (oi.KitParentId = kp.Id)
			WHERE oi.Id = @orderItemId);
END