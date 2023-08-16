using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.Common.EntityChangeProcessing.Helpers
{
    public class EntityChangeEvent<T>
    {
        public EntityChangeEvent(T entity, string externalId, bool isNew)
        {
            Entity = entity;
            ExternalId = externalId;
            IsNew = isNew;
        }

        public T Entity { get; }
        public string ExternalId { get; }
        public bool IsNew { get; }

        public string CustomData { get; set; }
    }
}
