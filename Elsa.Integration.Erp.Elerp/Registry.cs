using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Robowire;

namespace Elsa.Integration.Erp.Elerp
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<ElerpClient>().Use<ElerpClient>();
        }
    }
}
