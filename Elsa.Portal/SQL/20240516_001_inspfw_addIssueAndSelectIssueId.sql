IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'inspfw_addIssueAndSelectId')
	DROP PROCEDURE inspfw_addIssueAndSelectId;

GO

CREATE PROCEDURE inspfw_addIssueAndSelectId (@sessionId INT, @typeName NVARCHAR(200), @code NVARCHAR(200), @message NVARCHAR(1000))
AS
BEGIN
	DECLARE @issueId INT;
	
	EXEC @issueId = inspfw_addIssue @sessionID, @typeName, @code, @message;

	SELECT @issueId IssueId;
END