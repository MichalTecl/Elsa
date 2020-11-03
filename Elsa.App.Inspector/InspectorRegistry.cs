using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.App.Inspector.Repo;
using Robowire;

namespace Elsa.App.Inspector
{
    public class InspectorRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IInspectionsRepository>().Use<InspectionsRepository>();
        }
    }
}
