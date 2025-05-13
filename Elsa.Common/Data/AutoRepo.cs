using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.Core.Query.Filtering.QuerySegments.Misc;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Common.Data
{
    public class AutoRepo<T> where T:class, IIntIdEntity
    {
        private readonly ISession _session;
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly Func<IDatabase, IQueryBuilder<T>, IQueryBuilder<T>> _selectQueryModifier;
        private readonly TimeSpan? _cacheTtl;
        private readonly string _cacheKeySuffix;

        private static readonly object _boundKeysLock = new object();
        private static readonly HashSet<string> _boundCacheKeys = new HashSet<string>();

        public AutoRepo(ISession session, 
            IDatabase database, 
            ICache cache, 
            Func<IDatabase, IQueryBuilder<T>, IQueryBuilder<T>> selectQueryModifier = null, 
            int? cacheTtlSeconds = 600, 
            string cacheKeySuffix = "")
        {
            _session = session;
            _database = database;
            _cache = cache;
            _selectQueryModifier = selectQueryModifier;
            _cacheTtl = (cacheTtlSeconds ?? 0) <= 0 ? null : (TimeSpan?)TimeSpan.FromMinutes(cacheTtlSeconds.Value);
            _cacheKeySuffix = cacheKeySuffix;

            CacheKey = $"autorepo_{typeof(T).Name}_{_session.Project.Id}{_cacheKeySuffix}";
        }
               
        public string CacheKey { get; }

        public string BindCacheKey(string key)
        {
            lock (_boundKeysLock)
            {
                _boundCacheKeys.Add(key);
            }

            return key;
        }

        public IReadOnlyCollection<T> GetAll()
        {
            var getter = _database.SelectFrom<T>();

            if (_selectQueryModifier != null)
                getter = _selectQueryModifier(_database, getter);

            if (typeof(IProjectRelatedEntity).IsAssignableFrom(typeof(T)))
                getter = getter.Where(i => ((IProjectRelatedEntity)i).ProjectId == _session.Project.Id);

            if (_cacheTtl == null)
                return getter.Execute().ToList().AsReadOnly();

            return _cache.ReadThrough(CacheKey, _cacheTtl.Value, () => {
                return getter.Execute().ToList().AsReadOnly();
                });
        }

        public void ClearCache() 
        {
            lock (_boundKeysLock)
            {
                foreach (var bk in _boundCacheKeys)
                    _cache.Remove(bk);
            }

            _cache.Remove(CacheKey); 
        }

        public void UpdateWhere(Action<T> update, Func<T, bool> where)
        {
            using(var tx = _database.OpenTransaction())
            {
                var srcs = GetAll().Where(where);

                foreach (var src in srcs)
                    update(src);

                _database.SaveAll(srcs);

                tx.Commit();
            }

            ClearCache();
        }

        public T Upsert(int? id, Action<T> setup)
        {
            T record = default;
            using (var tx = _database.OpenTransaction())
            {
                record = GetAll().FirstOrDefault(r => r.Id == id);

                if (record == null)
                    record = _database.New<T>();

                setup(record);

                _database.Save(record);

                tx.Commit();
            }

            ClearCache();

            return record;
        }
                
        public void DeleteWhere(Func<T, bool> where)
        {
            using (var tx = _database.OpenTransaction())
            {
                var srcs = GetAll().Where(where);

                _database.DeleteAll(srcs);

                tx.Commit();
            }

            ClearCache();
        }

        public void Save(T entity)
        {
            if (entity is IProjectRelatedEntity pre)
                pre.ProjectId = _session.Project.Id;

            if (entity is IHasAuthor auth && entity.Id < 1)
                auth.AuthorId = _session.User.Id;

            _database.Save(entity);

            ClearCache();
        }

        public T Create(Action<T> setup)
        {
            var ett = _database.New<T>(setup);
            Save(ett);

            return ett;
        }

        public void GetOrCreate(Func<T, bool> where, Action<T> setup)
        {
            using (var tx = _database.OpenTransaction())
            {
                var srcs = GetAll().Where(where).ToList();
                                
                if (srcs.Count > 1)
                    throw new ArgumentException("Provided predicate matched more than 1 record");

                var src = srcs.SingleOrDefault() ?? _database.New<T>();

                setup(src);

                Save(src);

                tx.Commit();
            }

            ClearCache();
        }
    }
}
