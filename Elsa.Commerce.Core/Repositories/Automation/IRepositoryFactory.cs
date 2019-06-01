using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories.Automation
{
    public interface IRepositoryFactory
    {
        IRepository<T> GetForSmallTable<T>(Action<IQueryBuilder<T>> queryCustomization = null)
            where T : class, IIntIdEntity, IProjectRelatedEntity;
    }
}
