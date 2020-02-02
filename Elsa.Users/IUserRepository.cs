using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Users
{
    public interface IUserRepository
    {
        HashSet<string> GetUserRights(int userId);
    }
}
