using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;

namespace Elsa.Common.Caching
{
    public class PerProjectDbCache : IPerProjectDbCache
    {
        private static readonly Random s_rnd = new Random();
        private readonly ISession m_session;
        private readonly IDatabase m_database;
        private readonly ICache m_cache;

        public PerProjectDbCache(ISession session, IDatabase database, ICache cache)
        {
            m_session = session;
            m_database = database;
            m_cache = cache;
        }

        public IEnumerable<T> ReadThrough<T>(string key, Func<IDatabase, IQueryBuilder<T>> queryFactory) where T : class, IProjectRelatedEntity
        {
            return ReadThrough(key,
                () =>
                    {
                        var query = queryFactory(m_database);
                        query.Where(p => p.ProjectId == m_session.Project.Id);
                        return query.Execute();
                    });
        }

        public IEnumerable<TModel> ReadThroughWithConversion<TEntity, TModel>(string key, Func<IDatabase, IQueryBuilder<TEntity>> queryFactory, Func<TEntity, TModel> conversion) where TEntity : class, IProjectRelatedEntity
        {
            return ReadThrough(key, 
                () =>
                {
                    var query = queryFactory(m_database);
                    query.Where(p => p.ProjectId == m_session.Project.Id);
                    return query.Execute().Select(conversion);
                });
        }

        public T ReadThrough<T>(string key, Func<T> factory)
        {
            key = $"key_{key}_by_projectId={m_session.Project.Id}";

            return m_cache.ReadThrough(
                key,
                TimeSpan.FromMinutes(s_rnd.Next(8, 15)), 
                factory);
        }

        public void Remove(string key)
        {
            key = $"key_{key}_by_projectId={m_session.Project.Id}";
            m_cache.Remove(key);
        }

        public void Save<T>(T entity) where T : class, IProjectRelatedEntity
        {
            entity.ProjectId = m_session.Project.Id;
            m_database.Save(entity);
        }

        public T New<T>() where T : class
        {
            return m_database.New<T>();
        }

        public ITransaction OpenTransaction()
        {
            return m_database.OpenTransaction();
        }
    }
}
