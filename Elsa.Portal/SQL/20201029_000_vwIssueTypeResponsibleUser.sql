IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'vwIssueTypeResponsibleUser')
	DROP VIEW vwIssueTypeResponsibleUser;

GO

CREATE VIEW vwIssueTypeResponsibleUser
AS
SELECT it.Id  InspectionTypeId, 
       usr.Id UserId, 
	   ISNULL(mx.DaysAfterDetect, 0) DaysAfterDetect, 
	   ISNULL(mx.EMailOverride, usr.EMail) Email
	FROM InspectionType it
	LEFT JOIN InspectionResponsibilityMatrix mx ON (mx.InspectionTypeId = it.Id)
	LEFT JOIN [User] usr ON ((mx.Id IS NULL AND usr.ProjectId = it.ProjectId) OR mx.ResponsibleUserId = usr.Id);