IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'inspfw_getActiveIssuesSummary')
	DROP PROCEDURE inspfw_getActiveIssuesSummary;

GO

CREATE PROCEDURE inspfw_getActiveIssuesSummary (@projectId INT, @userId INT = NULL)
AS
BEGIN
    
	SELECT it.Id [Id], it.Name [Name], COUNT(DISTINCT ii.Id) [Count], MAX(ii.FirstDetectDt)
	  FROM InspectionIssue ii
	  JOIN InspectionType  it ON (ii.InspectionTypeId = it.Id)
	  JOIN vwIssueTypeResponsibleUser iru ON (it.Id = iru.InspectionTypeId)
	 WHERE 1 = 1
	   AND  it.ProjectId = @projectId
	   AND ii.ProjectId = @projectId
	   AND (ii.PostponedTill IS NULL OR ii.PostponedTill < GETDATE())
	   AND (ii.ResolveDt IS NULL OR ii.ResolveDt > GETDATE())
	   AND ((@userId IS NULL) OR (@userId = ISNULL(iru.UserId, @userId) AND ((GETDATE() - ISNULL(iru.DaysAfterDetect, 0)) >= ii.FirstDetectDt)))
     GROUP BY it.Id, it.Name
	 ORDER BY MAX(ii.FirstDetectDt) DESC;
END

GO
