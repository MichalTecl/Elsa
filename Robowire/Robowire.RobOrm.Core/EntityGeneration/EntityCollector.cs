using System;
using System.Collections.Generic;

namespace Robowire.RobOrm.Core.EntityGeneration
{
    public sealed class EntityCollector : IEntityCollector
    {
        private readonly IEnumerable<Type> m_types;

        public EntityCollector(IEnumerable<Type> types)
        {
            m_types = types;
        }

        public IEnumerable<Type> GetEntityTypes()
        {
            return m_types;
        }
    }
}
