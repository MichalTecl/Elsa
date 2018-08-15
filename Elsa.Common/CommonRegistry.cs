using Elsa.Common.Configuration;
using Elsa.Core.Entities.Commerce;

using Robowire;

namespace Elsa.Common
{
    public static class CommonRegistry
    {
        public static void SetupContainer(IContainer container)
        {
            ElsaDbInstaller.Initialize(container);
            
            container.Setup(c => c.For<IConfigurationRepository>().Use<ConfigurationRepository>());
        }
    }
}
