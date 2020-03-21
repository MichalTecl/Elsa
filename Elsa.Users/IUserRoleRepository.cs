using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Common.Security;
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

        IEnumerable<string> GetRoleRights(int roleId);

        IEnumerable<UserRightViewModel> GetEditableUserRights(int roleId);

        void AssignRoleRight(int roleId, string rightSymbol);

        void RemoveRoleRight(int roleId, string rightSymbol);

        IEnumerable<IUser> GetRoleMembers(int roleId);

        void AssignUserToRole(int roleId, int userId);

        void UnassignUserFromRole(int roleId, int userId);
    }
}
