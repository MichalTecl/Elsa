IF EXISTS(SELECT * FROM sys.procedures WHERE name = 'GetUserRights')
	DROP PROCEDURE GetUserRights;

GO

CREATE PROCEDURE GetUserRights(@userId INT)
AS
BEGIN
	SELECT DISTINCT uri.Symbol
	  FROM [User] u
	  JOIN UserRoleMember urm ON (urm.MemberId = u.Id)
	  JOIN UserRole ur ON (urm.RoleId = ur.Id)
	  JOIN UserRoleRight urr ON (ur.Id = urr.RoleId)
	  JOIN UserRight uri ON (urr.RightId = uri.Id)
	WHERE u.Id = @userId;
END
