using Elsa.App.Crm.DataReporting;
using Elsa.App.Crm.Repositories;
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
            setup.For<SalesRepRepository>().Use<SalesRepRepository>();
            setup.For<CustomerMeetingsRepository>().Use<CustomerMeetingsRepository>();
            setup.For<CustomerTagRepository>().Use<CustomerTagRepository>();
            setup.For<DistributorsRepository>().Use<DistributorsRepository>();
        }
    }
}
