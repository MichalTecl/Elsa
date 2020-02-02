using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Caching;

namespace Elsa.Users.Components
{
    public class UserRepository : IUserRepository
    {
        private readonly ICache m_cache;

        public UserRepository(ICache cache)
        {
            m_cache = cache;
        }

        public HashSet<string> GetUserRights(int userId)
        {
            throw new NotImplementedException();
        }
    }
}
