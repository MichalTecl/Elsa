using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.Crm.Raynet
{
    public class RaynetClientRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<RnProtocol>().Use<RnProtocol>();
            setup.For<IRaynetClient>().Use<RnActions>();
        }
    }
}
