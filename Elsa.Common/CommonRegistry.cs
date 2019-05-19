using Elsa.Common.Caching;
using Elsa.Common.Configuration;
using Elsa.Common.Logging;
using Elsa.Common.SysCounters;
using Elsa.Core.Entities.Commerce;

using Robowire;

namespace Elsa.Common
{
    public class CommonRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            AsyncLogger.Initialize(new ConnectionStringProvider());

            setup.For<IConfigurationRepository>().Use<ConfigurationRepository>();
            setup.For<ILog>().Use<Logger>();
            setup.For<ICache>().Use<Cache>();
            setup.For<IPerProjectDbCache>().Use<PerProjectDbCache>();
            setup.For<ISysCountersManager>().Use<SysCounterManager>();
        }
    }
}
