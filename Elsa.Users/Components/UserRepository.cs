using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Users.Entities;
using Elsa.Users.Infrastructure;
using Elsa.Users.ViewModel;
using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Users.Components
{
    public class UserRepository : IUserRepository, IUserRoleRepository
    {
        private readonly ICache m_cache;
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly IServiceLocator m_serviceLocator;

        public UserRepository(ICache cache, IDatabase database, ISession session, IServiceLocator serviceLocator)
        {
            m_cache = cache;
            m_database = database;
            m_session = session;
            m_serviceLocator = serviceLocator;
        }

        public HashSet<string> GetUserRights(int userId)
        {
            return m_cache.ReadThrough($"usrightsf_{userId}", TimeSpan.FromHours(1), () =>
            {
                var result = new HashSet<string>();
                m_database.Sql().Call("GetUserRights").WithParam("@userId", userId).ReadRows<string>(s => result.Add(s));

                return result;
            });
        }

        public RoleMap GetProjectRoles()
        {
            return m_cache.ReadThrough($"rolemap_{m_session.Project.Id}", TimeSpan.FromHours(24), () =>
            {
                var allRoles = new List<RoleMapNode>();

                m_database.Sql().ExecuteWithParams("SELECT Id, Name, ParentRoleId FROM UserRole WHERE ProjectId={0}", m_session.Project.Id)
                    .ReadRows<int, string, int?>((id, name, parentId) =>
                    {
                        var model = new RoleMapNode()
                        {
                            Id = id,
                            Name = name,
                            ParentRoleId = parentId
                        };

                        allRoles.Add(model);
                    });

                var map = new RoleMap();
                map.Import(allRoles);

                return map;
            });
        }

        public RoleMap GetRolesVisibleForUser(int userId)
        {
            return m_cache.ReadThrough($"rolemap_{m_session.Project.Id}_rolesvisiblefor_{userId}", TimeSpan.FromHours(24), () =>
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
            return m_cache.ReadThrough($"usroles_{userId}", TimeSpan.FromHours(1), () =>
            {
                var result = new List<int>();
                m_database.Sql().ExecuteWithParams("SELECT RoleId FROM UserRoleMember WHERE MemberId = {0}", userId).ReadRows<int>(
                    roleId =>
                    {
                        result.Add(roleId);
                    });
                return result;
            });
        }
        
        public void CreateRole(string name, int parentRoleId)
        {
            var existingRole = m_database.SelectFrom<IUserRole>()
                .Where(r => r.Name == name && r.ProjectId == m_session.Project.Id).Take(1).Execute().FirstOrDefault();

            if (existingRole != null)
            {
                throw new InvalidOperationException($"Role \"{name}\" jiz existuje");
            }

            var role = m_database.New<IUserRole>(r =>
            {
                r.Name = name;
                r.ParentRoleId = parentRoleId;
                r.ProjectId = m_session.Project.Id;
            });

            m_database.Save(role);

            InvalidateCache(true);
        }

        public void RenameRole(int roleId, string newName)
        {
            newName = newName?.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new InvalidOperationException("Název role nesmí být prázdný");
            }

            var visibleRoles = GetRolesVisibleForUser(m_session.User.Id);
            var toRename = visibleRoles.FindRole(roleId).Ensure();

            if (!toRename.CanEdit)
            {
                throw new InvalidOperationException("Nepovoleno");
            }

            var existingRole = m_database.SelectFrom<IUserRole>()
                .Where(r => r.ProjectId == m_session.Project.Id && r.Name == newName).Take(1).Execute()
                .FirstOrDefault();

            if ((existingRole != null) && (roleId != (existingRole?.Id ?? -1)))
            {
                throw new InvalidOperationException($"Role \"{newName}\" již existuje");
            }

            var role = m_database.SelectFrom<IUserRole>().Where(r => r.ProjectId == m_session.Project.Id)
                .Where(r => r.Id == roleId).Take(1).Execute().FirstOrDefault().Ensure();

            role.Name = newName;
            m_database.Save(role);

            InvalidateCache(true);
        }

        public void DeleteRole(int roleId)
        {
            var visibleRoles = GetRolesVisibleForUser(m_session.User.Id);

            var roleToDelete = visibleRoles.FindRole(roleId).Ensure();

            if (roleToDelete.ParentRoleId == null)
            {
                throw new InvalidOperationException("Cannot delete administrator role");
            }

            var childRole = m_database.SelectFrom<IUserRole>().Where(r => r.ParentRoleId == roleId).Take(1).Execute().FirstOrDefault();
            if (childRole != null)
            {
                throw new InvalidOperationException("Role má podřízené role");
            }

            var member = m_database.SelectFrom<IUserRoleMember>().Where(m => m.RoleId == roleId).Take(1).Execute().FirstOrDefault();
            if (member != null)
            {
                throw new InvalidOperationException("Role má přiřazeny uživatele");
            }

            using (var tx = m_database.OpenTransaction())
            {
                var rurToDel = m_database.SelectFrom<IUserRoleRight>().Where(r => r.RoleId == roleId).Execute();

                m_database.DeleteAll(rurToDel);

                var roleEntity = m_database.SelectFrom<IUserRole>()
                    .Where(r => r.ProjectId == m_session.Project.Id && r.Id == roleId).Execute().Take(1)
                    .FirstOrDefault().Ensure();

                m_database.Delete(roleEntity);

                tx.Commit();
            }
            
            InvalidateCache(true);
        }

        public IEnumerable<string> GetRoleRights(int roleId)
        {
            return m_database.Sql().Call("GetRoleRights").WithParam("@projectId", m_session.Project.Id).WithParam("@roleId", roleId)
                .MapRows<string>(r => r.GetString(0));
        }

        public IEnumerable<UserRightViewModel> GetEditableUserRights(int roleId)
        {
            var allRights = m_cache.ReadThrough("allUserRights", TimeSpan.FromDays(1), () =>
            {
                var sysRights = UserRights.All.ToList();

                var index = new Dictionary<string, int>(sysRights.Count);

                m_database.Sql().Call("SyncUserRights")
                    .WithStructuredParam("@rights", 
                        "dbo.StringTable", 
                        sysRights.Select(r => r.Symbol), 
                        new[] {"Val"},
                        s => new object[] {s})
                    .ReadRows<int, string>((id, symbol) => { index.Add(symbol, id); });

                return new UserRightMap(UserRights.All, index);
            });

            var userVisibleRoles = GetRolesVisibleForUser(m_session.User.Id);

            var role = userVisibleRoles.FindRole(roleId).Ensure($"Uživatel nemá potřebná oprávnění ke správě zvolené role");

            var parentRole = role.ParentRoleId == null
                ? role
                : userVisibleRoles.FindRole(role.ParentRoleId.Value).Ensure();

            var parentRoleRights =
                new HashSet<string>(GetRoleRights(parentRole.Id), StringComparer.InvariantCultureIgnoreCase);
            
            // get only rights visible for current user and for parent role
            var visibleRights = allRights.Clone(ur => m_session.HasUserRight(ur.Symbol) && parentRoleRights.Contains(ur.Symbol));

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
            
            var entity = m_database.New<IUserRoleRight>();
            entity.AssignDt = DateTime.Now;
            entity.AssignedById = m_session.User.Id;
            entity.RoleId = roleId;
            entity.RightId = editableRight.Id;
            
            m_database.Save(entity);

            InvalidateUserRightsCache(roleId);
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

            using (var tx = m_database.OpenTransaction())
            {
                foreach (var role in rolesToUnassign)
                {
                    foreach (var right in rightsToUnassign)
                    {
                        m_database.Sql()
                            .ExecuteWithParams("DELETE FROM UserRoleRight WHERE RoleId={0} AND RightId = {1}", role, right).NonQuery();
                    }
                }

                tx.Commit();
            }

            foreach (var r in rolesToUnassign)
            {
                InvalidateUserRightsCache(r);
            }
        }

        public IEnumerable<IUser> GetRoleMembers(int roleId)
        {
            return m_cache.ReadThrough($"roleMembers_{roleId}", TimeSpan.FromHours(1), () =>
                {
                    return m_database.SelectFrom<IUserRoleMember>().Join(m => m.Member)
                        .Where(m => m.ProjectId == m_session.Project.Id).Where(m => m.RoleId == roleId).Execute().Select(r => r.Member).OrderBy(u => u.EMail);
                });
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

        private void InvalidateUserRightsCache(int roleId)
        {
            var assignedUsers = m_database.SelectFrom<IUserRoleMember>().Where(rm => rm.RoleId == roleId).Execute();
            foreach (var au in assignedUsers)
            {
                m_cache.Remove($"usrightsf_{au.MemberId}");
            }
        }

        private void InvalidateCache(bool roles)
        {
            if (roles)
            {
                m_cache.RemoveByPrefix($"rolemap_{ m_session.Project.Id}");
            }
        }
    }
}
