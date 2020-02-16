using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Elsa.Users.Entities;
using Elsa.Users.ViewModel;
using Robowire.RobOrm.Core;

namespace Elsa.Users.Components
{
    public class UserRepository : IUserRepository, IUserRoleRepository
    {
        private readonly ICache m_cache;
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        public UserRepository(ICache cache, IDatabase database, ISession session)
        {
            m_cache = cache;
            m_database = database;
            m_session = session;
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
            return m_cache.ReadThrough($"rolemap_{m_session.Project.Id}", TimeSpan.MaxValue, () =>
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
            return m_cache.ReadThrough($"rolemap_{m_session.Project.Id}_rolesvisiblefor_{userId}", TimeSpan.MaxValue, () =>
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

                    visibleRoles.Add(r.Clone());
                }

                var submap = new RoleMap();
                submap.Import(visibleRoles);

                return submap;
            });
        }

        public IEnumerable<int> GetRoleIdsOfUser(int userId)
        {
            return m_cache.ReadThrough($"usroles_{userId}", TimeSpan.MaxValue, () =>
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

            if (roleId != (existingRole?.Id ?? -1))
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

        private void InvalidateCache(bool roles)
        {
            if (roles)
            {
                m_cache.RemoveByPrefix($"rolemap_{ m_session.Project.Id}");
            }
        }
    }
}
