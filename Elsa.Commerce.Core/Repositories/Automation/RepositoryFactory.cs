using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories.Automation
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly ICache m_cache;
        private readonly ISession m_session;
        private readonly IDatabase m_database;

        public RepositoryFactory(ISession session, IDatabase database, ICache cache)
        {
            m_session = session;
            m_database = database;
            m_cache = cache;
        }

        public IRepository<T> GetForSmallTable<T>(Action<IQueryBuilder<T>> queryCustomization = null)
            where T : class, IIntIdEntity, IProjectRelatedEntity
        {
            return new SimpleRepository<T>(m_cache, m_session, m_database, queryCustomization);
        }
    }
}
