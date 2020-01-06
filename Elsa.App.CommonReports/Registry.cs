using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robowire;

namespace Elsa.App.CommonReports
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IStockReportLoader>().Use<StockReportLoader>();
        }
    }
}
