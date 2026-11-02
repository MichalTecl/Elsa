using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Users.Entities;
using Elsa.Users.Infrastructure;
using Elsa.Users.ViewModel;
using Robowire;
using RobOrm.Core;

namespace Elsa.Users.Components
{
    public class UserRepository : IUserRepository, IUserRoleRepository
    {
        private readonly ICache _cache;
        private readonly IDatabase _database;
        private readonly ISession _session;

        public UserRepository(ICache cache, IDatabase database, ISession session)
        {
            _cache = cache;
            _database = database;
            _session = session;
        }

        public HashSet<string> GetUserRights(int userId)
        {
            return _cache.ReadThrough($"usrightsf_{userId}", TimeSpan.FromHours(1), () =>
            {
                var result = new HashSet<string>();
                _database.Sql().Call("GetUserRights").WithParam("@userId", userId).ReadRows<string>(s => result.Add(s));

                return result;
            });
        }

        public IEnumerable<IUser> GetAllUsers()
        {
            return _cache.ReadThrough($"allusers_{_session.Project.Id}", TimeSpan.FromMinutes(5),
                () => _database.SelectFrom<IUser>().Where(u => u.ProjectId == _session.Project.Id).Execute());
        }

        public void CreateUserAccount(string email, string plainPassword)
        {
            using (var tx = _database.OpenTransaction())
            {
                var existingUser = _database.SelectFrom<IUser>()
                    .Where(u => u.ProjectId == _session.Project.Id && u.EMail == email).Execute().FirstOrDefault();

                if (existingUser != null)
                {
                    throw new InvalidOperationException($"Uživatel již existuje");
                }

                var nuser = _database.New<IUser>();

                nuser.EMail = email;
                nuser.ParentId = _session.User.Id;
                nuser.PasswordHash = plainPassword;
                nuser.UsesDefaultPassword = true;
                nuser.ProjectId = _session.Project.Id;
                nuser.Salt = string.Empty;
                nuser.VerificationCode = string.Empty;

                _database.Save(nuser);

                tx.Commit();
            }

            _cache.Remove($"allusers_{_session.Project.Id}");
        }

        public void UpdateUser(int userId, Action<IUser> update)
        {
            if (!GetCanManage(_session.User.Id, userId))
            {
                throw new InvalidOperationException("unauthorized");
            }

            using (var tx = _database.OpenTransaction())
            {
                var user = _database.SelectFrom<IUser>().Where(u => u.ProjectId == _session.Project.Id)
                    .Where(u => u.Id == userId).Take(1).Execute().FirstOrDefault().Ensure();

                var oldPass = user.PasswordHash;

                update(user);

                if (user.PasswordHash != oldPass) 
                {                   
                    var hist = _database.New<IUserPasswordHistory>();
                    hist.InsertDt = DateTime.Now;
                    hist.UserId = userId;
                    hist.PasswordHash = user.PasswordHash;
                    _database.Save(hist);
                }

                _database.Save(user);

                LogoutUser(userId);

                tx.Commit();
            }

            InvalidateCache(userId: userId);
        }

        public bool GetCanManage(int managerId, int managedId)
        {
            if (managerId == managedId)
                return true;

            var allRoles = GetProjectRoles();

            foreach(var managerRole in allRoles.FindRolesByUserId(managerId))
                if (managerRole.ParentRoleId == null)
                {
                    return true;
                }

            var rolesVisibleForManager = GetRolesVisibleForUser(managerId);

            var allRolesOfManagedUser = GetRoleIdsOfUser(managedId);

            // Manager cannot manage account which has a role invisible for the manager

            foreach (var managedRoleId in allRolesOfManagedUser)
            {
                if (rolesVisibleForManager.FindRole(managedRoleId) == null)
                {
                    return false;
                }
            }

            return true;
        }

        public void InvalidateUserCache(int userId)
        {
            InvalidateCache(userId: userId);
        }

        public RoleMap GetProjectRoles()
        {
            return _cache.ReadThrough($"rolemap_{_session.Project.Id}", TimeSpan.FromHours(24), () =>
            {
                var allRoles = new List<RoleMapNode>();

                var roles = _database.SelectFrom<IUserRole>().Join(ur => ur.Members)
                    .Where(ur => ur.ProjectId == _session.Project.Id).Execute();

                foreach (var role in roles)
                {
                    var model = new RoleMapNode
                    {
                        Id = role.Id,
                        Name = role.Name,
                        ParentRoleId = role.ParentRoleId
                    };

                    model.MemberUserIds.AddRange(role.Members.Select(m => m.MemberId));

                    allRoles.Add(model);
                }
                
                var map = new RoleMap();
                map.Import(allRoles);

                return map;
            });
        }

        public RoleMap GetRolesVisibleForUser(int userId)
        {
            return _cache.ReadThrough($"rolemap_{_session.Project.Id}_rolesvisiblefor_{userId}", TimeSpan.FromHours(24), () =>
            {
                var allRoles = GetProjectRoles();
                var userRoles = GetRoleIdsOfUser(userId);

                var visibleRoles = new List<RoleMapNode>();

                foreach (var rId in userRoles)
                {
                    var r = allRoles.FindRole(rId);
                    if (r == null)
                    {
                        continue;
                    }

                    visibleRoles.Add(r);
                }

                var submap = new RoleMap();
                submap.Import(visibleRoles);

                return submap;
            });
        }

        public IEnumerable<int> GetRoleIdsOfUser(int userId)
        {
            return _cache.ReadThrough($"usroles_{userId}", TimeSpan.FromHours(1), () =>
            {
                var result = new List<int>();
                _database.Sql().ExecuteWithParams("SELECT RoleId FROM UserRoleMember WHERE MemberId = {0}", userId).ReadRows<int>(
                    roleId =>
                    {
                        result.Add(roleId);
                    });
                return result;
            });
        }
        
        public void CreateRole(string name, int parentRoleId)
        {
            var existingRole = _database.SelectFrom<IUserRole>()
                .Where(r => r.Name == name && r.ProjectId == _session.Project.Id).Take(1).Execute().FirstOrDefault();

            if (existingRole != null)
            {
                throw new InvalidOperationException($"Role \"{name}\" jiz existuje");
            }

            var role = _database.New<IUserRole>(r =>
            {
                r.Name = name;
                r.ParentRoleId = parentRoleId;
                r.ProjectId = _session.Project.Id;
            });

            _database.Save(role);

            InvalidateCache();
        }

        public void RenameRole(int roleId, string newName)
        {
            newName = newName?.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new InvalidOperationException("Název role nesmí být prázdný");
            }

            var visibleRoles = GetRolesVisibleForUser(_session.User.Id);
            var toRename = visibleRoles.FindRole(roleId).Ensure();

            if (!toRename.CanEdit)
            {
                throw new InvalidOperationException("Nepovoleno");
            }

            var existingRole = _database.SelectFrom<IUserRole>()
                .Where(r => r.ProjectId == _session.Project.Id && r.Name == newName).Take(1).Execute()
                .FirstOrDefault();

            if ((existingRole != null) && (roleId != existingRole.Id))
            {
                throw new InvalidOperationException($"Role \"{newName}\" již existuje");
            }

            var role = _database.SelectFrom<IUserRole>().Where(r => r.ProjectId == _session.Project.Id)
                .Where(r => r.Id == roleId).Take(1).Execute().FirstOrDefault().Ensure();

            role.Name = newName;
            _database.Save(role);

            InvalidateCache();
        }

        public void DeleteRole(int roleId)
        {
            var visibleRoles = GetRolesVisibleForUser(_session.User.Id);

            var roleToDelete = visibleRoles.FindRole(roleId).Ensure();

            if (roleToDelete.ParentRoleId == null)
            {
                throw new InvalidOperationException("Cannot delete administrator role");
            }

            var childRole = _database.SelectFrom<IUserRole>().Where(r => r.ParentRoleId == roleId).Take(1).Execute().FirstOrDefault();
            if (childRole != null)
            {
                throw new InvalidOperationException("Role má podřízené role");
            }

            var member = _database.SelectFrom<IUserRoleMember>().Where(m => m.RoleId == roleId).Take(1).Execute().FirstOrDefault();
            if (member != null)
            {
                throw new InvalidOperationException("Role má přiřazeny uživatele");
            }

            using (var tx = _database.OpenTransaction())
            {
                var rurToDel = _database.SelectFrom<IUserRoleRight>().Where(r => r.RoleId == roleId).Execute();

                _database.DeleteAll(rurToDel);

                var roleEntity = _database.SelectFrom<IUserRole>()
                    .Where(r => r.ProjectId == _session.Project.Id && r.Id == roleId).Execute().Take(1)
                    .FirstOrDefault().Ensure();

                _database.Delete(roleEntity);

                tx.Commit();
            }
            
            InvalidateCache(roleId);
        }

        public IEnumerable<string> GetRoleRights(int roleId)
        {
            return _database.Sql().Call("GetRoleRights").WithParam("@projectId", _session.Project.Id).WithParam("@roleId", roleId)
                .MapRows(r => r.GetString(0));
        }

        public IEnumerable<UserRightViewModel> GetEditableUserRights(int roleId)
        {
            var allRights = GetAllUserRights();

            var userVisibleRoles = GetRolesVisibleForUser(_session.User.Id);

            var role = userVisibleRoles.FindRole(roleId).Ensure("Uživatel nemá potřebná oprávnění ke správě zvolené role");

            var parentRole = role.ParentRoleId == null
                ? role
                : userVisibleRoles.FindRole(role.ParentRoleId.Value).Ensure();

            var parentRoleRights =
                new HashSet<string>(GetRoleRights(parentRole.Id), StringComparer.InvariantCultureIgnoreCase);
            
            // get only rights visible for current user and for parent role
            var visibleRights = allRights.Clone(ur => _session.HasUserRight(ur.Symbol) && parentRoleRights.Contains(ur.Symbol));

            var currentRoleRights = new HashSet<string>(GetRoleRights(roleId), StringComparer.InvariantCultureIgnoreCase);

            return visibleRights.GetAssignments(ur => currentRoleRights.Contains(ur.Symbol));
        }

        public void AssignRoleRight(int roleId, string rightSymbol)
        {
            var roleEditableRights = GetEditableUserRights(roleId).ToList();

            var editableRight = FindRight(roleEditableRights, rightSymbol).Ensure("Nepovoleno");

            if (editableRight.Assigned)
            {
                return;
            }

            //Check that parent right is assigned to the role
            var rightToCheckTheParent = editableRight;
            while (rightToCheckTheParent.ParentRightSymbol != null)
            {
                rightToCheckTheParent = FindRight(roleEditableRights, rightToCheckTheParent.ParentRightSymbol).Ensure();
                if (!rightToCheckTheParent.Assigned)
                {
                    throw new InvalidOperationException("Nepovoleno");
                }
            }
            
            var allRoles = GetProjectRoles();

            var thisRole = allRoles.FindRole(roleId);
            if (thisRole.ParentRoleId != null)
            {
                var parentRoleRights = new HashSet<string>(GetRoleRights(thisRole.ParentRoleId.Value), StringComparer.InvariantCultureIgnoreCase);
                if (!parentRoleRights.Contains(rightSymbol))
                {
                    throw new InvalidOperationException("Nepovoleno");
                }
            }
            
            var entity = _database.New<IUserRoleRight>();
            entity.AssignDt = DateTime.Now;
            entity.AssignedById = _session.User.Id;
            entity.RoleId = roleId;
            entity.RightId = editableRight.Id;
            
            _database.Save(entity);
            
            InvalidateCache();
        }
        
        public void RemoveRoleRight(int roleId, string rightSymbol)
        {
            var thisRole = GetProjectRoles().FindRole(roleId).Ensure();
            if (thisRole.ParentRoleId == null)
            {
                return;
            }

            var roleEditableRights = GetEditableUserRights(roleId).ToList();

            var editableRight = FindRight(roleEditableRights, rightSymbol).Ensure("Nepovoleno");
            if (!editableRight.Assigned)
            {
                return;
            }

            var rightsToUnassign = new HashSet<int>();
            editableRight.Visit(r => rightsToUnassign.Add(r.Id));

            

            var rolesToUnassign = new HashSet<int>();
            thisRole.Visit(r => rolesToUnassign.Add(r.Id));

            using (var tx = _database.OpenTransaction())
            {
                foreach (var role in rolesToUnassign)
                {
                    foreach (var right in rightsToUnassign)
                    {
                        _database.Sql()
                            .ExecuteWithParams("DELETE FROM UserRoleRight WHERE RoleId={0} AND RightId = {1}", role, right).NonQuery();
                    }
                }

                tx.Commit();
            }
            
            InvalidateCache();
        }

        public IEnumerable<IUser> GetRoleMembers(int roleId)
        {
            return _cache.ReadThrough($"roleMembers_{roleId}", TimeSpan.FromHours(1), () =>
                {
                    return _database.SelectFrom<IUserRoleMember>().Join(m => m.Member)
                        .Where(m => m.ProjectId == _session.Project.Id).Where(m => m.RoleId == roleId).Execute().Select(r => r.Member).OrderBy(u => u.EMail);
                });
        }

        public void AssignUserToRole(int roleId, int userId)
        {
            var allRoles = GetRolesVisibleForUser(_session.User.Id); 

            var desiredRole = allRoles.FindRole(roleId).Ensure(r => r.CanEdit, "Nepovoleno");
            if (desiredRole.MemberUserIds.Contains(userId))
            {
                // is already a member
                return;
            }

            var allUserRoles = allRoles.FindRolesByUserId(userId).ToList();

            // Check if user doesn't have any higher role
            foreach (var userRole in allUserRoles)
            {
                if (userRole.IsAncestorOf(roleId))
                {
                    throw new InvalidOperationException($"Uživatel již má roli {userRole.Name}, která je v hierarchii výše než požadovaná role {desiredRole.Name}. Pokud je toto snížení oprávnění požadováno, je třeba nejdříve odstranit uživatele z role {userRole.Name}");
                }
            }

            var toDelete = new HashSet<int>();

            //Find all user's lower roles which have to be upgraded to the desired one
            void CollectUpgrades(IEnumerable<RoleMapNode> level)
            {
                foreach (var r in level)
                {
                    if (r.MemberUserIds.Contains(userId))
                    {
                        toDelete.Add(r.Id);
                    }

                    CollectUpgrades(r.ChildRoles);
                }
            }

            CollectUpgrades(desiredRole.ChildRoles);

            using (var tx = _database.OpenTransaction())
            {
                var assignmentsToDelete = new List<IUserRoleMember>(toDelete.Count);

                foreach (var roletoUnassignId in toDelete)
                {
                    _cache.Remove($"roleMembers_{roletoUnassignId}");
                    assignmentsToDelete.AddRange(_database.SelectFrom<IUserRoleMember>().Where(m =>
                        m.RoleId == roletoUnassignId && m.MemberId == userId && m.ProjectId == _session.Project.Id).Execute());
                }

                _database.DeleteAll(assignmentsToDelete);

                var newBridge = _database.New<IUserRoleMember>();
                newBridge.MemberId = userId;
                newBridge.RoleId = desiredRole.Id;
                newBridge.IncludedById = _session.User.Id;
                newBridge.ProjectId = _session.Project.Id;
                newBridge.ValidFrom = DateTime.Now;
                
                _database.Save(newBridge);

                tx.Commit();
            }

            InvalidateCache(roleId, userId);
        }

        public void UnassignUserFromRole(int roleId, int userId)
        {
            var allRoles = GetRolesVisibleForUser(_session.User.Id);

            var desiredRole = allRoles.FindRole(roleId).Ensure(r => r.CanEdit, "Nepovoleno");
            if (!desiredRole.MemberUserIds.Contains(userId))
            {
                // already not a member
                return;
            }

            var ett = _database.SelectFrom<IUserRoleMember>().Where(urm => urm.ProjectId == _session.Project.Id)
                .Where(urm => urm.MemberId == userId && urm.RoleId == roleId).Execute().FirstOrDefault().Ensure();

            _database.Delete(ett);

            InvalidateCache(roleId, userId);
        }

        private UserRightViewModel FindRight(IEnumerable<UserRightViewModel> source, string symbol)
        {
            foreach (var r in source)
            {
                if (r.Symbol.Equals(symbol, StringComparison.InvariantCultureIgnoreCase))
                {
                    return r;
                }

                var subRes = FindRight(r.ChildRights, symbol);
                if (subRes != null)
                {
                    return subRes;
                }
            }

            return null;
        }
        
        private void InvalidateCache(int? roleId = null, int? userId = null)
        {
            if (_session?.Project == null)
            {
                _cache.Clear();
                return;
            }

            _cache.RemoveByPrefix($"rolemap_{ _session.Project.Id}");

            if (roleId != null)
            {
                _cache.Remove($"roleMembers_{roleId}");

                if (userId == null)
                {
                    var assignedUsers = _database.SelectFrom<IUserRoleMember>().Where(rm => rm.RoleId == roleId)
                        .Execute();
                    foreach (var au in assignedUsers)
                    {
                        _cache.Remove($"usrightsf_{au.MemberId}");
                    }
                }
            }

            if (userId != null)
            {
                _cache.Remove($"usrightsf_{userId}");
            }
            else
            {
                _cache.RemoveByPrefix("usrightsf_");
            }

            _cache.Remove($"allusers_{_session.Project.Id}");
        }

        private void LogoutUser(int userId)
        {
            var openSessions = _database.SelectFrom<IUserSession>().Where(us => us.ProjectId == _session.Project.Id)
                .Where(us => us.EndDt == null).Where(us => us.UserId == userId).Execute().ToList();

            foreach (var openSession in openSessions)
            {
                openSession.EndDt = DateTime.Now;
                _cache.Remove($"usix_{openSession.PublicId}");
                _database.Save(openSession);
            }
        }

        public IEnumerable<IUserPasswordHistory> GetPasswordHistory(int userId)
        {
            return _database.SelectFrom<IUserPasswordHistory>().Where(h => h.UserId == userId).OrderByDesc(u => u.InsertDt).Take(100).Execute();
        }

        public UserRightMap GetAllUserRights()
        {
            return _cache.ReadThrough("allUserRights", TimeSpan.FromDays(1), () =>
            {
                var sysRights = UserRights.All.ToList();

                var index = new Dictionary<string, int>(sysRights.Count);

                _database.Sql().Call("SyncUserRights")
                    .WithStructuredParam("@rights",
                        "dbo.StringTable",
                        sysRights.Select(r => r.Symbol),
                        new[] { "Val" },
                        s => new object[] { s })
                    .ReadRows<int, string>((id, symbol) => { index.Add(symbol, id); });

                return new UserRightMap(UserRights.All, index);
            });
        }

        public IEnumerable<IUser> GetUsersByRight(UserRight right)
        {            
            // TODO may need to be optimized if used frequently
            var allUsers = GetAllUsers();

            foreach (var user in allUsers)
            {
                if (GetUserRights(user.Id).Contains(right.Symbol))
                {
                    yield return user;
                }
            }
        }
    }
}
