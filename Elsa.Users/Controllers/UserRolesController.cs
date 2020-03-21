using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Users.ViewModel;
using Robowire.RoboApi;

namespace Elsa.Users.Controllers
{
    [Controller("UserRoles")]
    public class UserRolesController : ElsaControllerBase
    {
        private readonly IUserRoleRepository m_userRoleRepository;
        private readonly IUserRepository m_userRepository;
        private readonly ISession m_session;

        public UserRolesController(IWebSession webSession, ILog log, IUserRoleRepository userRoleRepository, IUserRepository userRepository) : base(webSession, log)
        {
            m_userRoleRepository = userRoleRepository;
            m_userRepository = userRepository;
            m_session = webSession;
        }

        public RoleMap GetRoles()
        {
            return m_userRoleRepository.GetRolesVisibleForUser(m_session.User.Id);
        }

        public RoleMap AddChildRole(int parentId, string name)
        {
            m_userRoleRepository.CreateRole(name, parentId);
            return GetRoles();
        }

        public RoleMap RenameRole(int roleId, string newName)
        {
            m_userRoleRepository.RenameRole(roleId, newName);
            return GetRoles();
        }

        public RoleMap DeleteRole(int roleId)
        {
            m_userRoleRepository.DeleteRole(roleId);
            return GetRoles();
        }

        public IEnumerable<UserRightViewModel> GetRoleRightsEditor(int roleId)
        {
            return m_userRoleRepository.GetEditableUserRights(roleId);
        }

        public IEnumerable<UserRightViewModel> ChangeRoleRightAssignment(int roleId, string rightSymbol, bool assign)
        {
            if (assign)
            {
                m_userRoleRepository.AssignRoleRight(roleId, rightSymbol);
            }
            else
            {
                m_userRoleRepository.RemoveRoleRight(roleId, rightSymbol);
            }

            return GetRoleRightsEditor(roleId);
        }

        public IEnumerable<UserViewModel> GetRoleMembers(int roleId)
        {
            var allUsers = m_userRoleRepository.GetRoleMembers(roleId);

            foreach (var u in allUsers)
            {
                if (u.Id == m_session.User.Id)
                {
                    continue;
                }

                yield return new UserViewModel()
                {
                    Id = u.Id,
                    Name = u.EMail
                };
            }
        }

        public IEnumerable<UserViewModel> AssignUser(int roleId, string userName)
        {
            var urid = m_userRepository.GetAllUsers()
                .FirstOrDefault(u => u.EMail.Equals(userName, StringComparison.InvariantCultureIgnoreCase))
                .Ensure("Uzivatel neexistuje").Id;

            m_userRoleRepository.AssignUserToRole(roleId, urid);

            return GetRoleMembers(roleId);
        }

        public IEnumerable<UserViewModel> UnassignUser(int roleId, int userId)
        {
            m_userRoleRepository.UnassignUserFromRole(roleId, userId);

            return GetRoleMembers(roleId);
        }
    }
}
