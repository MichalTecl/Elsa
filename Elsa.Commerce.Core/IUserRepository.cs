using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common.Security;

namespace Elsa.Commerce.Core 
{
    public interface IUserRepository : IUserNickProvider
    {        
        string GetUserEmail(int userId);

        IUser GetUser(int id);
        List<IUser> GetAllUsers();

        DataIndex<int, IUser> GetUserIndex();
    }
}
