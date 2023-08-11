IF EXISTS(SELECT * FROM sys.procedures WHERE name = 'GetUserRights')
	DROP PROCEDURE GetUserRights;

GO

CREATE PROCEDURE GetUserRights(@userId INT)
AS
BEGIN
    IF EXISTS(SELECT TOP 1 1 
	            FROM UserRole ur
			    JOIN UserRoleMember urm ON (urm.RoleId = ur.Id)
               WHERE ur.ParentRoleId IS NULL
			     AND urm.MemberId = @userId)
	BEGIN
		SELECT Symbol FROM UserRight;
		RETURN;
	END

	SELECT DISTINCT uri.Symbol
	  FROM [User] u
	  JOIN UserRoleMember urm ON (urm.MemberId = u.Id)
	  JOIN UserRole ur ON (urm.RoleId = ur.Id)
	  JOIN UserRoleRight urr ON (ur.Id = urr.RoleId)
	  JOIN UserRight uri ON (urr.RightId = uri.Id)
	WHERE u.Id = @userId;
END
