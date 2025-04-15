IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_unexpectedPaymentMethod')
	DROP PROCEDURE insp_unexpectedPaymentMethod;

GO

CREATE PROCEDURE insp_unexpectedPaymentMethod (@sessionId INT, @projectId INT, @retryIssueId INT = null)
AS
BEGIN
	
	/*
	const string issueTypeColumn = "IssueType";
	const string issueCodeColumn = "IssueCode";
	const string messageColumn = "Message";
	const string issueDataPrefix = "data:";
	const string actionControlPrefix = "ActionControlUrl";
	const string actionNamePrefix = "ActionName";
	*/
    
	-- DECLARE @projectId INT = 1


	SELECT N'Neočekávaná platební metoda' IssueType,
	       'unexpectedPaymentMethod_' + po.OrderNumber IssueCode,
		   N'Zákazník ' 
		   + po.CustomerName 
		   + N' patří do kategorie "' 
		   + cgt.ErpGroupName 
		   + N'", je tedy očekávána platební metoda "' 
		   + cgt.DefaultPaymentMethod 
		   + N'". Objednávka ' 
		   + po.OrderNumber  
		   + N' má ale platební metodu "'
		   + po.PaymentMethodName
		   + '"' Message,
    '/UI/Inspector/ActionControls/Ignore.html' "ActionControlUrl_Ignore",
			N'Ignorovat' "ActionName_Ignore"
	
  FROM PurchaseOrder po  
  JOIN Customer c ON (c.ErpUid = po.CustomerErpUid)    
  JOIN (SELECT g.CustomerId, MAX(t.Id) GroupTypeId
          FROM CustomerGroup g
		  JOIN CustomerGroupType t ON (t.ErpGroupName = g.ErpGroupName)
		 WHERE t.DefaultPaymentMethod IS NOT NULL
		GROUP BY g.CustomerId) groupType ON (groupType.CustomerId = c.Id)
   JOIN CustomerGroupType cgt ON (cgt.Id = groupType.GroupTypeId)
 WHERE po.OrderStatusId < 3       
 AND NOT EXISTS(SELECT TOP 1 1
                FROM CustomerGroup cg
				JOIN CustomerGroupType cgt ON (cg.ErpGroupName = cgt.ErpGroupName)
			   WHERE cg.CustomerId = c.Id
			     AND cgt.DefaultPaymentMethod IS NOT NULL
				 AND cgt.DefaultPaymentMethod = po.PaymentMethodName)



END
GO