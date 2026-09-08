
CREATE OR ALTER PROCEDURE insp_unpaidOrders (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN

DECLARE @maxDays INT = 14;

SELECT N'Chybí platba (' + po.PaymentMethodName + ')'  IssueType,
       'unpaidOrder' + po.OrderNumber IssueCode,
       N'Objednávka ' + po.OrderNumber + ' ' + po.CustomerName + ' nebyla uhrazena více než ' + LTRIM(STR(@maxDays)) + ' dnů.' [Message],
       po.Id "data:OrderId",
       '/UI/OrdersInfo/InspectorActions/UnpaidOrderCancelControl.html' "ActionControlUrl_CancelUnpaidOrder",
       'UNPAID_ORDER_CANCEL' "ActionName_CancelUnpaidOrder"
FROM PurchaseOrder po
WHERE po.OrderStatusId = 2
  AND DATEADD(DAY, @maxDays, po.PurchaseDate) < GETDATE();
END

