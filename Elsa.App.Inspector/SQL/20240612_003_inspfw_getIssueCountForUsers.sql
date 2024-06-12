IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'inspfw_getIssueCountForUsers')
	DROP PROCEDURE inspfw_getIssueCountForUsers;

GO;

CREATE PROCEDURE inspfw_getIssueCountForUsers (@projectId INT)
AS
BEGIN
	SELECT allUsr.ResponsibleUserId [UserId], ISNULL(uisu.Count, 0)
	  FROM 
		(SELECT DISTINCT irm.ResponsibleUserId 
		  FROM InspectionResponsibilityMatrix irm
		  JOIN [User] irmu ON (irm.ResponsibleUserId = irmu.Id)
		  WHERE irmu.ProjectId = @projectId) allUsr
	  LEFT JOIN (
		SELECT iru.UserId [UserId], COUNT(DISTINCT ii.Id) [Count]
			  FROM InspectionIssue ii
			  JOIN InspectionType  it ON (ii.InspectionTypeId = it.Id)
			  JOIN vwIssueTypeResponsibleUser iru ON (it.Id = iru.InspectionTypeId)	 
			 WHERE 1 = 1
			   AND  it.ProjectId = @projectId
			   AND ii.ProjectId = @projectId
			   AND (ii.PostponedTill IS NULL OR ii.PostponedTill < GETDATE())
			   AND (ii.ResolveDt IS NULL OR ii.ResolveDt > GETDATE())	   
			 GROUP BY iru.UserId) uisu ON (uisu.UserId = allUsr.ResponsibleUserId);
END

