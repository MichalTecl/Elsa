using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Core.Entities.Commerce.Common.Security;

namespace Elsa.Commerce.Core
{
    public interface IUserRepository
    {
        string GetUserNick(int userId);

        string GetUserEmail(int userId);

        IUser GetUser(int id);
        List<IUser> GetAllUsers();
    }
}
