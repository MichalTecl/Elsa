using Elsa.App.Crm.DataReporting;
using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm
{
    public class CrmRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<DatasetLoader>().Use<DatasetLoader>();
        }
    }
}
