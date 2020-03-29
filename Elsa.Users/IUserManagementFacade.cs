using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Users
{
    public interface IUserManagementFacade
    {
        void InviteUser(string email);
        void ResetPassword(int userId);
        void SetAccountLocked(int userId, bool locked);
    }
}
