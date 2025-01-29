IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'insp_missingSalesRep')
	DROP PROCEDURE insp_missingSalesRep;

GO

CREATE PROCEDURE insp_missingSalesRep (@sessionId INT, @projectId INT, @retryIssueId INT = null)
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
	
	SELECT N'VO bez OZ' IssueType,
		   N'invalidSalesRepsCount_' + LTRIM(STR(c.Id)) IssueCode,
		   N'VO "' + ISNULL(c.Name, '?') + N'" nemá OZ' [Message]
	  FROM Customer c
	  LEFT JOIN CustomerGroup cg ON c.id = cg.CustomerId
	 WHERE c.ProjectId = @projectId
	   AND cg.ErpGroupName = N'Přátelé a parťáci'
	   AND NOT EXISTS(SELECT TOP 1 1
						FROM SalesRepCustomer src
					   WHERE src.CustomerId = c.Id)
	   AND LastDeactivationDt IS NULL
	   AND IsCompany = 1;
END



