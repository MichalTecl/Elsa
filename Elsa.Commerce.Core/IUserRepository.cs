using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core
{
    public interface IUserRepository
    {
        string GetUserNick(int userId);

        string GetUserEmail(int userId);
    }
}
