using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Common.Security;

namespace Elsa.Commerce.Core.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IPerProjectDbCache m_cache;
        
        public UserRepository(IPerProjectDbCache cache)
        {
            m_cache = cache;
        }

        public string GetUserNick(int userId)
        {
            return GetUserEmail(userId).Split('@').FirstOrDefault();
        }

        public string GetUserEmail(int userId)
        {
            var user = GetAllUsers().FirstOrDefault(u => u.Id == userId);
            if (string.IsNullOrWhiteSpace(user?.EMail))
            {
                return "?";
            }

            return user.EMail;
        }

        public IUser GetUser(int id)
        {
            return GetAllUsers().FirstOrDefault(u => u.Id == id);
        }

        private IEnumerable<IUser> GetAllUsers()
        {
            return m_cache.ReadThrough("allUsers", database => database.SelectFrom<IUser>());
        }
    }
}
