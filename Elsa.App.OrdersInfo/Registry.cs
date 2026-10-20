using Elsa.App.OrdersInfo.App;
using Elsa.App.OrdersInfo.App.Aggregations;
using Robowire;

namespace Elsa.App.OrdersInfo
{
    public class OrdersInfoRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<OrdersInfoRepository>().Use<OrdersInfoRepository>();
            setup.For<OrdersInfoXlsExporter>().Use<OrdersInfoXlsExporter>();
            setup.For<OrderItemBatchAssignmentEditor>().Use<OrderItemBatchAssignmentEditor>();

            setup.For<OrdersAggregatorFactory>().Use<OrdersAggregatorFactory>();

        }
    }
}
