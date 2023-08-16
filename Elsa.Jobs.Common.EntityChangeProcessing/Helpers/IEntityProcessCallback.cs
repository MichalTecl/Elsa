using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.Common.EntityChangeProcessing.Helpers
{
    public interface IEntityProcessCallback<T>
    {
        void OnProcessed(T entity, string obtainedExternalId, string customData);
    }
}
