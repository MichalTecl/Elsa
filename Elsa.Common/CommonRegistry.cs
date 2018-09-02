using Elsa.Common.Configuration;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce;

using Robowire;

namespace Elsa.Common
{
    public static class CommonRegistry
    {
        public static void SetupContainer(IContainer container)
        {
            ElsaDbInstaller.Initialize(container);

            AsyncLogger.Initialize(new ConnectionStringProvider());
            
            container.Setup(c => c.For<IConfigurationRepository>().Use<ConfigurationRepository>());
            container.Setup(c => c.For<ILog>().Use<Logger>());

        }
    }
}
