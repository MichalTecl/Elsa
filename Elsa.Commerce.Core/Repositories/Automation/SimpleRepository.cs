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
    internal class SimpleRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IIntIdEntity, IProjectRelatedEntity
    {
        private static readonly string s_cacheKey = $"srb_{typeof(TEntity).Name}_{{0}}";
        private readonly ICache _cache;
        private readonly ISession _session;
        private readonly Action<IQueryBuilder<TEntity>> _queryCustomization;
        private readonly IDatabase _database;

        public SimpleRepository(ICache cache, ISession session, IDatabase database, Action<IQueryBuilder<TEntity>> queryCustomization = null)
        {
            _cache = cache;
            _session = session;
            _database = database;

            queryCustomization = queryCustomization ?? (q => { });

            _queryCustomization = queryCustomization;
        }

        private string CacheKey => string.Format(s_cacheKey, _session.Project.Id);

        public IQueryBuilder<TEntity> Query
        {
            get
            {
                var query = _database.SelectFrom<TEntity>().Where(e => e.ProjectId == _session.Project.Id);
                _queryCustomization(query);

                return query;
            }
        }

        public IEnumerable<TEntity> All()
        {
            return _cache.ReadThrough(CacheKey, TimeSpan.FromHours(1), () => Query.Execute().ToList());
        }

        public TEntity Get(int id, bool throwIfNotFound = true)
        {
            var result = All().FirstOrDefault(e => e.Id == id);

            if ((result == null) && throwIfNotFound)
            {
                throw new InvalidOperationException($"{typeof(TEntity).Name}.Id = {id} was not found");
            }

            return result;
        }

        public void Delete(int id)
        {
            using (var tx = _database.OpenTransaction())
            {
                var entity = Get(id, false);
                if (entity != null)
                {
                    _database.Delete(entity);
                    _cache.Remove(CacheKey);
                }

                tx.Commit();
            }
        }

        public TEntity Set(int? id, Action<TEntity> setup)
        {
            using (var tx = _database.OpenTransaction())
            {
                var entity = id == null ? _database.New<TEntity>() : Get(id.Value);

                setup(entity);

                entity.ProjectId = _session.Project.Id;

                _database.Save(entity);
                _cache.Remove(CacheKey);

                var result = Get(entity.Id);

                tx.Commit();

                return result;
            }
        }

        public void ClearCache()
        {
            _cache.Remove(CacheKey);
        }
    }
}
