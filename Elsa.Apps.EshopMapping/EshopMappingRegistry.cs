using Elsa.Apps.EshopMapping.Internal;
using Robowire;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Apps.EshopMapping
{
    public class EshopMappingRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IEshopMappingFacade>().Use<EshopMappingFacade>();
        }
    }
}
