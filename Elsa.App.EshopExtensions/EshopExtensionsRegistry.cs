using Elsa.App.EshopExtensions.Internal;
using Robowire;

namespace Elsa.App.EshopExtensions
{
    public class EshopExtensionsRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IEshopExtensionsRepository>().Use<EshopExtensionsRepository>();
        }
    }
}
