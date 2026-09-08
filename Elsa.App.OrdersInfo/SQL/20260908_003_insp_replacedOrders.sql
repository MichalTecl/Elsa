
CREATE OR ALTER PROCEDURE insp_replacedOrders (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN

    DECLARE @minDt DATETIME;

    SELECT @minDt = MIN(PurchaseDate)
      FROM PurchaseOrder 
     WHERE PurchaseDate > DATEADD(year, -1, GETDATE());
  
    SELECT N'Opakovaná objednávka' IssueType,
           'replacedOrder' + LTRIM(STR(older.Id)) IssueCode,
           N'Objednávka ' 
            + older.OrderNumber 
            + ' z ' 
            + FORMAT(older.PurchaseDate, 'dd.MM.yyyy HH:mm')
            + ' je ve stavu ' 
            + older.ErpStatusName
            + ', ale stejný e-mail <a target="_blank" href="/UI/OrdersInfo/OrdersInfo.html?CustomerNameWildcard=' + older.CustomerEmail + '">' + older.CustomerEmail + '</a> objednal později objednávku '
            + newer.OrderNumber + ' z ' + FORMAT(newer.PurchaseDate, 'dd.MM.yyyy HH:mm')
            + ', která je ve stavu ' + newer.ErpStatusName [Message],
            older.Id "data:OrderId",
            older.OrderNumber "data:OrderNumber",
            '/UI/OrdersInfo/InspectorActions/CancelReplacedOrderControl.html' "ActionControlUrl_CancelReplacedOrder",
           'REPLACED_ORDER_CANCEL' "ActionName_CancelReplacedOrder"
      FROM PurchaseOrder older
      JOIN PurchaseOrder newer ON (newer.CustomerEmail = older.CustomerEmail AND newer.PurchaseDate > older.PurchaseDate)
     WHERE older.OrderStatusId < 3 -- new, pending payment
       ANd newer.OrderStatusId < 6

  END 
   
   


   