IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_untrimmedProductName')
	DROP PROCEDURE insp_untrimmedProductName;

GO

CREATE PROCEDURE [insp_untrimmedProductName] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
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

	
	SELECT DISTINCT N'Mezera před/za názvem produktu' IssueType,
	                'productUntrimmedSpace_' + oi.PlacedName IssueCode,
					N'Mezera před/za názvem produktu "' + oi.PlacedName + '"' Message
	  FROM OrderItem oi
	  JOIN vwOrderItems oio ON (oi.Id = oio.OrderItemId)
	  JOIN PurchaseOrder po ON (oio.OrderId = po.Id)
	 WHERE TRIM(oi.PlacedName) <> oi.PlacedName
	   AND po.OrderStatusId < 5

END
GO




