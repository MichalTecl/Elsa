IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_getIssuesToMail')
	DROP PROCEDURE inspfw_getIssuesToMail;

GO

CREATE PROCEDURE inspfw_getIssuesToMail (@projectId INT)
AS
BEGIN
	SELECT isu.Id IssueId,
	       isu.Message,
		   iru.Email,
		   itp.Name TypeName
	  FROM InspectionIssue isu
	  JOIN InspectionType itp ON (isu.InspectionTypeId = itp.Id)
	  JOIN vwIssueTypeResponsibleUser iru ON (isu.InspectionTypeId = iru.InspectionTypeId)
	WHERE isu.ProjectId = @projectId
	  AND (isu.FirstDetectDt + iru.DaysAfterDetect) <= GETDATE()
	  AND ((isu.PostponedTill IS NULL) OR (isu.PostponedTill <= GETDATE()))
	  AND ((isu.ResolveDt IS NULL) OR (isu.ResolveDt > GETDATE()))
	  AND NOT EXISTS(SELECT TOP 1 1 
	                   FROM InspectionMailingHistory hi
					   WHERE hi.IssueId = isu.Id
					     AND hi.EMail = iru.Email);
END

GO

