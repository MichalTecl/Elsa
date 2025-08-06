IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'LoadTagAssignmentsInfo')
	DROP PROCEDURE LoadTagAssignmentsInfo;

GO

CREATE PROCEDURE LoadTagAssignmentsInfo (@customerIds IntTable READONLY)
AS
BEGIN

	SELECT asg.CustomerId    CustomerId,
		   u.EMail           AssignedBy,
		   asg.AssignDt      AssignDt,
		   asg.TagTypeId     TagTypeId,
           asg.Note          Note,
		   ct.Name           TagTypeName,
		   ct.DaysToWarning  DaysToWarning,
		   ct.CssClass       TagTypeCssClass,
		   ct.GroupId        TagTypeGroupId,
		   (ISNULL(trans.x, 0) / ISNULL(trans.x, 1)) HasTransitions,
           ISNULL(ct.RequiresNote, 0) RequiresNote
	  FROM CustomerTagAssignment asg
	  JOIN @customerIds cid ON (cid.Id = asg.CustomerId) 
	  JOIN CustomerTagType ct ON (asg.TagTypeId = ct.Id)
	  JOIN [User] u ON (asg.AuthorId = u.Id)
	  LEFT JOIN (SELECT ctt.SourceTagTypeId, MIN(ctt.Id) x
				   FROM CustomerTagTransition ctt  
				  GROUP BY ctt.SourceTagTypeId) trans ON (trans.SourceTagTypeId = asg.TagTypeId)   
      WHERE asg.UnassignDt IS NULL;

END  