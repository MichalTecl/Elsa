IF OBJECT_ID('dbo.GetProductOrderingInfo', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetProductOrderingInfo;
GO


CREATE PROCEDURE GetProductOrderingInfo (@erpId INT)
AS
BEGIN
    
    SELECT oi.PlacedName, 
           COUNT(po.Id) AS OrderCount, 
           MAX(po.BuyDate) AS LastOrder
    FROM OrderItem oi
    JOIN vwOrderItems oit ON (oi.Id = oit.OrderItemId)
    JOIN PurchaseOrder po ON (oit.OrderId = po.Id)
    WHERE 
        po.ErpId = @erpId
        AND po.BuyDate > DATEADD(year, -2, GETDATE())
    GROUP BY oi.PlacedName;
END;
