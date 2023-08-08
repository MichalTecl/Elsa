using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Users.ViewModel;

namespace Elsa.Users
{
    public interface IUserRepository
    {
        HashSet<string> GetUserRights(int userId);

        IEnumerable<IUser> GetAllUsers();

        void CreateUserAccount(string email, string plainPassword);

        void UpdateUser(int userId, Action<IUser> update);

        bool GetCanManage(int managerId, int managedId);

        void InvalidateUserCache(int userId);

        IEnumerable<IUserPasswordHistory> GetPasswordHistory(int userId);
    }
}
