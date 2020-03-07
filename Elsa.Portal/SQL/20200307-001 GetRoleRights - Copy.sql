IF EXISTS(SELECT * FROM sys.procedures WHERE name = 'GetRoleRights')
	DROP PROCEDURE GetRoleRights;

GO

CREATE PROCEDURE GetRoleRights(@roleId INT, @projectId INT)
AS
BEGIN
	SELECT DISTINCT uri.Symbol
	  FROM UserRole ur 
	  JOIN UserRoleRight urr ON (ur.Id = urr.RoleId)
	  JOIN UserRight uri ON (urr.RightId = uri.Id)
	WHERE ur.ProjectId = @projectId
	  AND ur.Id = @roleId;
END
