IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_missingMaterialOrder')
	DROP PROCEDURE insp_missingMaterialOrder;

GO

CREATE PROCEDURE [insp_missingMaterialOrder] (@sessionId INT, @projectId INT, @retryIssueId INT = null)
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

	WITH cte 
	AS
	(
	SELECT m.Name, moe.OrderDt, DATEADD(day, ISNULL(m.OrderFulfillDays, 30), moe.OrderDt) fulfillLimit
	  FROM MaterialOrderEvent moe
	  JOIN Material m ON (m.Id = moe.MaterialId)
	  WHERE NOT EXISTS(SELECT TOP 1 1 
	                     FROM MaterialBatch mb
						WHERE mb.MaterialId = moe.MaterialId
						  AND mb.Created > moe.InsertDt)	  
    )

	SELECT N'Nedošlo k nasladnění suroviny' IssueType,
	       'missingMaterialReceival_' + cte.Name IssueCode,
		     cte.Name 
		    + N': bylo očekáváno naskladnění do '
			+ FORMAT(cte.FulfillLimit, 'dd.MM.yyyy') Message	
  FROM cte
  WHERE cte.fulfillLimit < GETDATE()



END
GO


