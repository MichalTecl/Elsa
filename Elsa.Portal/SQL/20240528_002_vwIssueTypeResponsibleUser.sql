IF EXISTS(SELECT * FROM sys.views WHERE name = 'vwIssueTypeResponsibleUser')
	DROP VIEW vwIssueTypeResponsibleUser;

GO

CREATE VIEW [vwIssueTypeResponsibleUser]
AS
SELECT DISTINCT *
FROM 
(
	SELECT it.Id  InspectionTypeId, 
		   usr.Id UserId, 
		   ISNULL(mx.DaysAfterDetect, 0) DaysAfterDetect, 
		   ISNULL(mx.EMailOverride, usr.EMail) Email
		FROM InspectionType it
		INNER JOIN InspectionResponsibilityMatrix mx ON (mx.InspectionTypeId = it.Id)
		INNER JOIN [User] usr ON (mx.ResponsibleUserId = usr.Id)
	UNION
	SELECT it.Id InspectionTypeId,
		   u.Id  UserId,
		   0 DaysAfterDetect,
		   u.EMail Email
	  FROM InspectionType it
	  JOIN SysConfig cfg ON (cfg.ProjectId = it.ProjectId AND cfg.[Key] = 'Inspector.Superadmin')
	  JOIN [User] u ON (u.ProjectId = it.ProjectId AND u.EMail = REPLACE(cfg.ValueJson, '"', ''))
) x


