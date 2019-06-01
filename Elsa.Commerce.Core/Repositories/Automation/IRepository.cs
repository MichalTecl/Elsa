using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories.Automation
{
    public interface IRepository<TEntity>
        where TEntity : class, IIntIdEntity, IProjectRelatedEntity
    {
        IQueryBuilder<TEntity> Query { get; }

        IEnumerable<TEntity> All();

        TEntity Get(int id, bool throwIfNotFound = true);

        void Delete(int id);

        TEntity Set(int? id, Action<TEntity> setup);

        void ClearCache();
    }
}