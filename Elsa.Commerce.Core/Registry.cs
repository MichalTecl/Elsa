﻿using Elsa.Commerce.Core.Crm;
using Elsa.Commerce.Core.Impl;
using Elsa.Commerce.Core.Repositories;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Commerce.Core.Warehouse.Impl;

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
            setup.For<IOrderStatusTranslator>().Use<OrderStatusTranslator>();
            setup.For<IOrderStatusRepository>().Use<OrderStatusRepository>();
            setup.For<IPaymentRepository>().Use<PaymentRepository>();
            setup.For<IOrdersFacade>().Use<OrdersFacade>();
            setup.For<IVirtualProductRepository>().Use<VirtualProductRepository>();
            setup.For<IErpRepository>().Use<ErpRepository>();
            setup.For<IUnitRepository>().Use<UnitRepository>();
            setup.For<IUnitConversionHelper>().Use<UnitConversionHelper>();
            setup.For<IMaterialRepository>().Use<MaterialRepository>();
            setup.For<IVirtualProductFacade>().Use<VirtualProductFacade>();
            setup.For<IMaterialFacade>().Use<MaterialFacade>();
            setup.For<IKitProductRepository>().Use<KitProductRepository>();
            setup.For<ICustomerRepository>().Use<CustomerRepository>();
            setup.For<IUserRepository>().Use<UserRepository>();
            setup.For<IMaterialBatchRepository>().Use<MaterialBatchRepository>();
        }
    }
}
