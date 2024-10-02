using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;

namespace Elsa.Common.Caching
{
    public interface IPerProjectDbCache
    {
        IEnumerable<T> ReadThrough<T>(string key, Func<IDatabase, IQueryBuilder<T>> query)
            where T : class, IProjectRelatedEntity;

        IEnumerable<TModel> ReadThroughWithConversion<TEntity, TModel>(string key, Func<IDatabase, IQueryBuilder<TEntity>> query, Func<TEntity, TModel> conversion)
            where TEntity : class, IProjectRelatedEntity;

        T ReadThrough<T>(string key, Func<T> factory);
        T ReadThrough<T>(string key, TimeSpan timeout, Func<T> factory);

        void Remove(string key);

        void Save<T>(T entity) where T : class, IProjectRelatedEntity;

        T New<T>() where T : class;

        ITransaction OpenTransaction();
    }
}
