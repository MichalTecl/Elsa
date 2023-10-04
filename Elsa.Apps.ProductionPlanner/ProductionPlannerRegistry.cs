using Elsa.Apps.ProductionPlanner.Repositories;
using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ProductionPlanner
{
    public class ProductionPlannerRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IMonthlySalesMultiplierRepository>().Use<MonthlySalesMultipierRepository>();
        }
    }
}
