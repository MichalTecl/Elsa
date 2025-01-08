using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Interfaces
{
    public interface IUserNickProvider
    {
        string GetUserNick(int userId);
    }
}
