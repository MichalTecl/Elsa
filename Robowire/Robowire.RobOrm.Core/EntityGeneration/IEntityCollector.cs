using System;
using System.Collections.Generic;

namespace Robowire.RobOrm.Core.EntityGeneration
{
    public interface IEntityCollector
    {
        IEnumerable<Type> GetEntityTypes();
    }
}
