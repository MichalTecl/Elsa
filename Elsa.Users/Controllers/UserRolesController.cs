using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Users.ViewModel;
using Robowire.RoboApi;

namespace Elsa.Users.Controllers
{
    [Controller("UserRoles")]
    public class UserRolesController : ElsaControllerBase
    {
        private readonly IUserRoleRepository m_userRoleRepository;
        private readonly ISession m_session;

        public UserRolesController(IWebSession webSession, ILog log, IUserRoleRepository userRoleRepository) : base(webSession, log)
        {
            m_userRoleRepository = userRoleRepository;
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
    }
}
