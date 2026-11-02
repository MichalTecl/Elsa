using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Core;

using RobOrm.Core;

namespace Elsa.Common.Caching
{
    public class PerProjectDbCache : IPerProjectDbCache
    {
        private static readonly Random s_rnd = new Random();
        private readonly ISession _session;
        private readonly IDatabase _database;
        private readonly ICache _cache;

        public PerProjectDbCache(ISession session, IDatabase database, ICache cache)
        {
            _session = session;
            _database = database;
            _cache = cache;
        }

        public IEnumerable<T> ReadThrough<T>(string key, Func<IDatabase, IQueryBuilder<T>> queryFactory) where T : class, IProjectRelatedEntity
        {
            return ReadThrough(key,
                () =>
                    {
                        var query = queryFactory(_database);
                        query.Where(p => p.ProjectId == _session.Project.Id);
                        return query.Execute();
                    });
        }

        public IEnumerable<TModel> ReadThroughWithConversion<TEntity, TModel>(string key, Func<IDatabase, IQueryBuilder<TEntity>> queryFactory, Func<TEntity, TModel> conversion) where TEntity : class, IProjectRelatedEntity
        {
            return ReadThrough(key, 
                () =>
                {
                    var query = queryFactory(_database);
                    query.Where(p => p.ProjectId == _session.Project.Id);
                    return query.Execute().Select(conversion);
                });
        }

        public T ReadThrough<T>(string key, TimeSpan timeout, Func<T> factory)
        {
            key = $"key_{key}_by_projectId={_session.Project.Id}";

            return _cache.ReadThrough(
                key,
                timeout,                
                factory);
        }

        public T ReadThrough<T>(string key, Func<T> factory)
        {
            return ReadThrough(key, TimeSpan.FromMinutes(s_rnd.Next(8, 15)), factory);
        }

        public void Remove(string key)
        {
            key = $"key_{key}_by_projectId={_session.Project.Id}";
            _cache.Remove(key);
        }

        public void Save<T>(T entity) where T : class, IProjectRelatedEntity
        {
            entity.ProjectId = _session.Project.Id;
            _database.Save(entity);
        }

        public T New<T>() where T : class
        {
            return _database.New<T>();
        }

        public ITransaction OpenTransaction()
        {
            return _database.OpenTransaction();
        }
    }
}
