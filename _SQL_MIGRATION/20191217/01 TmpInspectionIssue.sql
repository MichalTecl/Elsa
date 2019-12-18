IF NOT EXISTS(SELECT TOP 1 1 FROM sys.tables WHERE name = 'InspectionIssueBuffer')
BEGIN
	CREATE TABLE [dbo].[InspectionIssueBuffer](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[InspectionType] [nvarchar](200) NOT NULL,
		[IssueCode] [nvarchar](200) NOT NULL,
		[Message] [nvarchar](1000) NOT NULL,
		[Created] [datetime] NOT NULL,
		[ProjectId] [int] NOT NULL);
END

GO

IF NOT EXISTS(SELECT TOP 1 1 FROM sys.tables WHERE name = 'InspectionSession')
BEGIN
	CREATE TABLE [dbo].[InspectionSession](	
	    [Id] INT IDENTITY(1,1),
		[InspectionType] [nvarchar](200) NOT NULL,	
		[Created] [datetime] NOT NULL,
		[Closed] [datetime] NULL,
		[ProjectId] [int] NOT NULL);
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'inspfw_openSession')
BEGIN
	DROP PROCEDURE inspfw_openSession;
END

GO

CREATE PROCEDURE inspfw_openSession
(
	@projectId INT,
	@inspectionType NVARCHAR(200)
)
AS
BEGIN
	IF EXISTS(SELECT TOP 1 1 FROM InspectionSession WHERE Closed IS NULL)
	BEGIN
		THROW 50001, 'Open session exists', 1;
	END

	INSERT INTO InspectionSession (InspectionType, Created, ProjectId)
	VALUES (@inspectionType, GETDATE(), @projectId);
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'inspfw_closeSession')
BEGIN
	DROP PROCEDURE inspfw_closeSession;
END

GO

CREATE PROCEDURE inspfw_closeSession
AS
BEGIN
	DECLARE @sid INT;
	DECLARE @projid INT;
	DECLARE @inspectionType NVARCHAR(200);

	SELECT TOP 1 @sid = Id, @projid = ProjectId, @inspectionType = InspectionType FROM InspectionSession WHERE Closed IS NULL;
	IF (@sid IS NULL)
	BEGIN
		THROW 50001, 'Open session does not exist', 1;
	END

	-- delete issues which were not detected anymore
	DELETE FROM InspectionIssue WHERE Id IN (
	SELECT isu.Id
	  FROM InspectionIssue isu
	  WHERE isu.ProjectId = @projid
	    AND isu.InspectionType = @inspectionType
		AND isu.IssueCode NOT IN (SELECT buf.IssueCode 
		                            FROM InspectionIssueBuffer buf));

	
   
    MERGE InspectionIssue AS target
	USING(
		SELECT @inspectionType InspType, buf.IssueCode, buf.Message, buf.Created, @projid ProjId
		FROM InspectionIssueBuffer buf) AS source
    ON (    source.ProjId = target.ProjectId 
	    AND source.InspType = target.InspectionType
	    AND source.IssueCode = target.IssueCode)
    WHEN MATCHED THEN 
						UPDATE SET target.Message = source.Message,
	                             target.Created = source.Created
    WHEN NOT MATCHED THEN
						INSERT (InspectionType, IssueCode, Message, Created, ProjectId)
						VALUES (source.InspType, source.IssueCode, source.Message, source.Created, source.ProjId);
	      
								  
	TRUNCATE TABLE InspectionIssueBuffer;
     
END

