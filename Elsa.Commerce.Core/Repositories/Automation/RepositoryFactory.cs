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

using RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories.Automation
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly ICache _cache;
        private readonly ISession _session;
        private readonly IDatabase _database;

        public RepositoryFactory(ISession session, IDatabase database, ICache cache)
        {
            _session = session;
            _database = database;
            _cache = cache;
        }

        public IRepository<T> GetForSmallTable<T>(Action<IQueryBuilder<T>> queryCustomization = null)
            where T : class, IIntIdEntity, IProjectRelatedEntity
        {
            return new SimpleRepository<T>(_cache, _session, _database, queryCustomization);
        }
    }
}
