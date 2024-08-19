using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Reporting
{
    public class ReportingRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<Repo.ReportRepository>().Use<Repo.ReportRepository>();
        }
    }
}
