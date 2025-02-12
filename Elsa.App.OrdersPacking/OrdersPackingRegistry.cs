using Elsa.App.OrdersPacking.App;
using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.OrdersPacking
{
    public class OrdersPackingRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<OrderReviewRepository>().Use<OrderReviewRepository>();
        }
    }
}
