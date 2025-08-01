using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.SyncErpCustomers
{
    public class CustomersSyncJobRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<CustomersSyncJob>().Use<CustomersSyncJob>();
        }
    }
}
