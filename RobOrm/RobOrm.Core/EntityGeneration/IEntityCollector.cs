using System;
using System.Collections.Generic;

namespace RobOrm.Core.EntityGeneration
{
    public interface IEntityCollector
    {
        IEnumerable<Type> GetEntityTypes();
    }
}
