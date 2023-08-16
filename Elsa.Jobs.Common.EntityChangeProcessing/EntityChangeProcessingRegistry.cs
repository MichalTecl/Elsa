using Elsa.Jobs.Common.EntityChangeProcessing.Entities;
using Elsa.Jobs.Common.EntityChangeProcessing.Helpers;
using Robowire;

namespace Elsa.Jobs.Common.EntityChangeProcessing
{
    public class EntityChangeProcessingRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IChangeProcessorHostFactory>().Use<ChangeProcessorHostFactory>();
        }
    }
}
