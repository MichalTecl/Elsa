using Elsa.Commerce.Core.Impl;
using Elsa.Commerce.Core.Repositories;

using Robowire;

namespace Elsa.Commerce.Core
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IErpClientFactory>().Use<ErpClientFactory>();
            setup.For<IPurchaseOrderRepository>().Use<PurchaseOrderRepository>();
            setup.For<ICurrencyRepository>().Use<CurrencyRepository>();
            setup.For<IProductRepository>().Use<ProductRepository>();
            setup.For<IOrderStatusMappingRepository>().Use<OrderStatusMappingRepository>();
            setup.For<IProductMappingRepository>().Use<ProductMappingRepository>();
            setup.For<IOrderStatusTranslator>().Use<OrderStatusTranslator>();
            setup.For<IOrderStatusRepository>().Use<OrderStatusRepository>();
        }
    }
}
