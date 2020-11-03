IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_addIssue')
	DROP PROCEDURE inspfw_addIssue;

GO

CREATE PROCEDURE inspfw_addIssue (@sessionId INT, @typeName NVARCHAR(200), @code NVARCHAR(200), @message NVARCHAR(1000))
AS
BEGIN
	
	DECLARE @projectId INT;
	DECLARE @procName NVARCHAR(300);

	SELECT TOP 1 @projectId = ise.ProjectId,
	             @procName = ise.CurrentProcName 
	  FROM InspectionSession ise
	 WHERE ise.Id = @sessionId;  

    IF (@projectId IS NULL OR @procName IS NULL)
	BEGIN
		THROW 51000, 'Invalid session', 1;
	END
	
	DECLARE @typeId INT = (SELECT TOP 1 Id FROM InspectionType WHERE Name = @typeName AND ProjectId = @projectId);
	IF (@typeId IS NULL)
	BEGIN
		INSERT INTO InspectionType (Name, ProjectId, LastRun, LastSessionId)
		VALUES (@typeName, @projectId, GETDATE(), @sessionId);

		SET @typeId = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		IF NOT EXISTS(SELECT TOP 1 1 
		                FROM InspectionType it
					   WHERE it.Id = @typeId
					     AND it.LastSessionId = @sessionId)
		BEGIN
			UPDATE InspectionType
			   SET LastRun = GETDATE(),
			       LastSessionId = @sessionId
			 WHERE Id = @typeId;
		END		                    
	END

	DECLARE @issueId INT = (SELECT TOP 1 ii.Id
	                          FROM InspectionIssue ii
							 WHERE ii.ProjectId = @projectId
							   AND ii.InspectionTypeId = @typeId
							   AND ii.IssueCode = @code);

	IF (@issueId IS NULL)
	BEGIN
		INSERT INTO InspectionIssue (InspectionTypeId, IssueCode, Message, FirstDetectDt, LastDetectDt, ProjectId, LastSessionId, ProcName)
		VALUES (@typeId, @code, @message, GETDATE(), GETDATE(), @projectId, @sessionId, @procName);	
		
		SET @issueId = SCOPE_IDENTITY();
			
	END
	ELSE
	BEGIN
		UPDATE InspectionIssue
		   SET Message = @message,
		       LastDetectDt = GETDATE(),
			   LastSessionId = @sessionId,
			   ResolveDt = null
		 WHERE Id = @issueId;

		 DELETE FROM InspectionIssueData WHERE IssueId = @issueId;
		 DELETE FROM InspectionIssueActionMenu WHERE  IssueId = @issueId;
	END

	RETURN @issueId;
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_setIssueAction')
	DROP PROCEDURE inspfw_setIssueAction;

GO

CREATE PROCEDURE inspfw_setIssueAction (@issueId INT, @controlUrl NVARCHAR(300), @actionName NVARCHAR(300))
AS
BEGIN	

	INSERT INTO InspectionIssueActionMenu (IssueId, ControlUrl, ActionName)
	SELECT @issueId, @controlUrl, @actionName
	  WHERE NOT EXISTS(SELECT TOP 1 1 
	                     FROM InspectionIssueActionMenu iam
						WHERE iam.IssueId = @issueId
						  AND iam.ControlUrl = @controlUrl);
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_getInspectionProcedures')
	DROP PROCEDURE inspfw_getInspectionProcedures;

GO

CREATE PROCEDURE inspfw_getInspectionProcedures 
AS
BEGIN
	SELECT name 
	  FROM sys.procedures sp
     WHERE sp.name like 'insp[_]%';
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_setIssueDataString')
	DROP PROCEDURE inspfw_setIssueDataString;

GO

CREATE PROCEDURE inspfw_setIssueDataString (@issueId INT, @property NVARCHAR(100), @value NVARCHAR(1000), @isArray BIT = 0)
AS
BEGIN									
	INSERT INTO InspectionIssueData (IssueId, PropertyName, StrValue, IsArray)
	VALUES (@issueId, @property, @value, @isArray);	
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_setIssueDataInt')
	DROP PROCEDURE inspfw_setIssueDataInt;

GO

CREATE PROCEDURE inspfw_setIssueDataInt (@issueId INT, @property NVARCHAR(100), @value INT, @isArray BIT = 0)
AS
BEGIN	
	INSERT INTO InspectionIssueData (IssueId, PropertyName, IntValue, IsArray)
	VALUES (@issueId, @property, @value, @isArray);	
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_closeSession')
	DROP PROCEDURE inspfw_closeSession;

GO

CREATE PROCEDURE inspfw_closeSession (@projectId INT, @sessionId INT, @forIssueId INT = NULL)
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
		      		
     DELETE FROM InspectionIssueData WHERE IssueId IN (SELECT Id FROM @outdatedIssues);     
	 DELETE FROM InspectionIssueActionMenu WHERE IssueId IN (SELECT Id FROM @outdatedIssues);
	 DELETE FROM InspectionIssue WHERE Id IN (SELECT Id FROM @outdatedIssues);
	 
	 DELETE FROM InspectionSession
	   WHERE ProjectId = @projectId
	     AND Id NOT IN(
		    SELECT isu.LastSessionId FROM InspectionIssue isu
			UNION
			SELECT it.LastSessionId FROM InspectionType it);
END

GO


IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_openSession')
	DROP PROCEDURE inspfw_openSession;

GO

CREATE PROCEDURE inspfw_openSession (@projectId INT)
AS
BEGIN

	DECLARE @existingSid INT;
	DECLARE @existingSdate DATETIME;
	
	SELECT TOP 1 @existingSid = ise.Id,
	             @existingSdate = ise.StartDt
	    FROM InspectionSession ise
	WHERE ise.ProjectId = @projectId
		AND ise.EndDt IS NULL;

	IF (@existingSid IS NOT NULL)
	BEGIN
		
		IF (DATEDIFF(hour, @existingSdate, GETDATE()) > 1)
		BEGIN
			UPDATE InspectionSession 
			   SET EndDt = GETDATE()
			 WHERE Id = @existingSid;
		END
		ELSE
		BEGIN
			SELECT -1;
			RETURN;
		END
	END
	
	INSERT INTO InspectionSession (StartDt, ProjectId, CurrentProcName)
	  VALUES (GETDATE(), @projectId, '');

    SELECT CAST(SCOPE_IDENTITY() as int);
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'inspfw_runInspection')
	DROP PROCEDURE inspfw_runInspection;

GO

CREATE PROCEDURE inspfw_runInspection (@projectId INT, @sessionId INT, @retryIssueId INT = null, @spName VARCHAR(300) = null)
AS
BEGIN
	IF (@spName IS NULL)
	BEGIN
		SET @spName = (SELECT TOP 1 ProcName FROM InspectionIssue WHERE ProjectId = @projectId AND Id = @retryIssueId);
	END

	IF (@spName IS NULL)
	BEGIN
		THROW 51000, 'Cannot obtain the procedure', 1;
	END

	UPDATE InspectionSession
	   SET CurrentProcName = @spName
	 WHERE Id = @sessionId;

	exec @spName @sessionId=@sessionId, @projectId=@projectId, @retryIssueId = @retryIssueId;
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_getActiveIssuesSummary')
	DROP PROCEDURE inspfw_getActiveIssuesSummary;

GO

CREATE PROCEDURE inspfw_getActiveIssuesSummary (@projectId INT, @userId INT = NULL)
AS
BEGIN
	SELECT it.Id [Id], it.Name [Name], COUNT(DISTINCT ii.Id) [Count], MAX(ii.FirstDetectDt)
	  FROM InspectionIssue ii
	  JOIN InspectionType  it ON (ii.InspectionTypeId = it.Id)
	  JOIN vwIssueTypeResponsibleUser iru ON (it.Id = iru.UserId)
	 WHERE it.ProjectId = @projectId
	   AND ii.ProjectId = @projectId
	   AND (ii.PostponedTill IS NULL OR ii.PostponedTill < GETDATE())
	   AND (ii.ResolveDt IS NULL OR ii.ResolveDt > GETDATE())
	   AND ((@userId IS NULL) OR (@userId = iru.UserId AND ((GETDATE() - iru.DaysAfterDetect) >= ii.FirstDetectDt)))
     GROUP BY it.Id, it.Name
	 ORDER BY MAX(ii.FirstDetectDt) DESC;
END

GO
