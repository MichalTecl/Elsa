using System;
using System.Collections.Generic;

namespace Robowire.Core
{
    public interface ILocatorFactory
    {
        IServiceLocator CreateLocatorInstance(IServiceLocator parentLocator, Dictionary<string, Func<IServiceLocator, object>> factories);
    }
}
