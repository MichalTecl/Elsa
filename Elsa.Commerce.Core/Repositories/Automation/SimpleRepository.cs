using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories.Automation
{
    internal class SimpleRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IIntIdEntity, IProjectRelatedEntity
    {
        private static readonly string s_cacheKey = $"srb_{typeof(TEntity).Name}_{{0}}";
        private readonly ICache m_cache;
        private readonly ISession m_session;
        private readonly Action<IQueryBuilder<TEntity>> m_queryCustomization;
        private readonly IDatabase m_database;

        public SimpleRepository(ICache cache, ISession session, IDatabase database, Action<IQueryBuilder<TEntity>> queryCustomization = null)
        {
            m_cache = cache;
            m_session = session;
            m_database = database;

            queryCustomization = queryCustomization ?? (q => { });

            m_queryCustomization = queryCustomization;
        }

        private string CacheKey => string.Format(s_cacheKey, m_session.Project.Id);

        public IQueryBuilder<TEntity> Query
        {
            get
            {
                var query = m_database.SelectFrom<TEntity>().Where(e => e.ProjectId == m_session.Project.Id);
                m_queryCustomization(query);

                return query;
            }
        }

        public IEnumerable<TEntity> All()
        {
            return m_cache.ReadThrough(CacheKey, TimeSpan.FromHours(1), () => Query.Execute().ToList());
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
            using (var tx = m_database.OpenTransaction())
            {
                var entity = Get(id, false);
                if (entity != null)
                {
                    m_database.Delete(entity);
                    m_cache.Remove(CacheKey);
                }

                tx.Commit();
            }
        }

        public TEntity Set(int? id, Action<TEntity> setup)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var entity = id == null ? m_database.New<TEntity>() : Get(id.Value);

                setup(entity);

                entity.ProjectId = m_session.Project.Id;

                m_database.Save(entity);
                m_cache.Remove(CacheKey);

                var result = Get(entity.Id);

                tx.Commit();

                return result;
            }
        }

        public void ClearCache()
        {
            m_cache.Remove(CacheKey);
        }
    }
}
