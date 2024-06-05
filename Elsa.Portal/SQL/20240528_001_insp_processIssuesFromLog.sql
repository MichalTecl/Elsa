IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERe name = 'insp_processIssuesFromLog')
	DROP PROCEDURE insp_processIssuesFromLog;

GO	

CREATE PROCEDURE insp_processIssuesFromLog (@sessionId INT, @projectId INT, @retryIssueId INT = null)
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

	SELECT i.IssueTypeName IssueType,
	       i.IssueCode IssueCode,
		   i.Message Message
	  FROM LogStoredInspectionIssue i
	 WHERE i.ProjectId = @projectId
	   AND i.InspectorProcessedDt IS NULL;

	UPDATE LogStoredInspectionIssue
	   SET InspectorProcessedDt = GETDATE()
	 WHERE InspectorProcessedDt IS NULL
	   AND ProjectId = @projectId;
END
GO
