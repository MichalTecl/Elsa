IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'inspfw_closeSession')
	DROP PROCEDURE [inspfw_closeSession];
GO

CREATE PROCEDURE [dbo].[inspfw_closeSession] (@projectId INT, @sessionId INT, @forIssueId INT = NULL)
AS
BEGIN	
    
	IF NOT EXISTS(SELECT TOP 1 1 
	                FROM InspectionSession ise
				   WHERE ise.Id = @sessionId
				     AND ise.ProjectId = @projectId
					 AND ise.EndDt IS NULL)
    BEGIN
		THROW 51000, 'Invalid session', 1;
	END

	UPDATE InspectionSession
	   SET EndDt = GETDATE()
	 WHERE Id = @sessionId;

	UPDATE InspectionIssue
	   SET ResolveDt = GETDATE()
	 WHERE ResolveDt IS NULL
	   AND ProjectId = @projectId
	   AND LastSessionId <> @sessionId
	   AND ((@forIssueId IS NULL) OR (Id = @forIssueId));  

	IF (@forIssueId IS NOT NULL)
	BEGIN
		RETURN;
	END

    DECLARE @outdatedIssues TABLE (IssueId INT);
	INSERT INTO @outdatedIssues
	    SELECT Id
		  FROM InspectionIssue 
		 WHERE  ResolveDt IS NOT NULL AND ResolveDt < (GETDATE() - 365);

	 DELETE FROM InspectionIssueActionsHistory WHERE InspectionIssueId IN (SELECT x.IssueId FROM @outdatedIssues x);     		      		
     DELETE FROM InspectionIssueData WHERE IssueId IN (SELECT x.IssueId FROM @outdatedIssues x);     
	 DELETE FROM InspectionIssueActionMenu WHERE IssueId IN (SELECT x.IssueId FROM @outdatedIssues x);
	 DELETE FROM InspectionIssue WHERE Id IN (SELECT x.IssueId FROM @outdatedIssues x);
	 
	 DELETE FROM InspectionSession
	   WHERE ProjectId = @projectId
	     AND Id NOT IN(
		    SELECT isu.LastSessionId FROM InspectionIssue isu
			UNION
			SELECT it.LastSessionId FROM InspectionType it);
END

