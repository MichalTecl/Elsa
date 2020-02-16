using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Users.ViewModel;

namespace Elsa.Users
{
    public interface IUserRoleRepository
    {
        RoleMap GetProjectRoles();

        RoleMap GetRolesVisibleForUser(int userId);

        IEnumerable<int> GetRoleIdsOfUser(int userId);

        void CreateRole(string name, int parentRoleId);

        void RenameRole(int roleId, string newName);

        void DeleteRole(int roleId);
    }
}
