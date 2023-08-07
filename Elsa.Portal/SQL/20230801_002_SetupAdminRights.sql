INSERT INTO UserRoleRight (roleId, rightId, AssignedById, AssignDt)
SELECT rol.Id roleId, rig.Id rightId, 2, GETDATE()
  FROM UserRole rol
  JOIN UserRight rig ON (1=1)
 WHERE rol.Name = 'Admin'
   ANd NOT EXISTS (SELECT TOP 1 1 
                     FROM UserRoleRight urr 
					WHERE urr.RoleId = rol.Id
					  AND urr.RightId = rig.Id);