IF EXISTS(SELECT * FROM sys.procedures WHERE name = 'GetRoleRights')
	DROP PROCEDURE GetRoleRights;

GO

CREATE PROCEDURE GetRoleRights(@roleId INT, @projectId INT)
AS
BEGIN
    IF EXISTS(SELECT TOP 1 1 
	            FROM UserRole adminur 
			   WHERE adminur.ProjectId = @projectId
			     AND adminur.ParentRoleId IS NULL
				 AND adminur.Id = @roleId)
	BEGIN
		SELECT Symbol FROM UserRight;
		RETURN;
	END
	
	SELECT DISTINCT uri.Symbol
	  FROM UserRole ur 
	  JOIN UserRoleRight urr ON (ur.Id = urr.RoleId)
	  JOIN UserRight uri ON (urr.RightId = uri.Id)
	WHERE ur.ProjectId = @projectId
	  AND ur.Id = @roleId;
END