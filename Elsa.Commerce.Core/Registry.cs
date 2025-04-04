﻿using Elsa.App.ImportExport;
using Elsa.Commerce.Core.Crm;
using Elsa.Commerce.Core.CurrencyRates;
using Elsa.Commerce.Core.Impl;
using Elsa.Commerce.Core.ImportExportModules;
using Elsa.Commerce.Core.Production;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Commerce.Core.Repositories;
using Elsa.Commerce.Core.Repositories.Automation;
using Elsa.Commerce.Core.SaleEvents;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Commerce.Core.Warehouse.BatchReporting;
using Elsa.Commerce.Core.Warehouse.Impl;
using Elsa.Commerce.Core.Warehouse.Thresholds;
using Elsa.Common.Interfaces;
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
            setup.For<IMaterialBatchFacade>().Use<MaterialBatchFacade>();
            setup.For<AmountProcessor>().Use<AmountProcessor>();
            setup.For<IPackingPreferredBatchRepository>().Use<PreferredBatchRepository>();
            setup.For<IProductionFacade>().Use<ProductionFacade>();
            setup.For<IBatchReportingFacade>().Use<BatchReportingFacade>();
            setup.For<IMaterialThresholdRepository>().Use<MaterialThresholdRepository>();
            setup.For<IStockEventRepository>().Use<StockEventRepository>();
            setup.For<ISupplierRepository>().Use<SupplierRepository>();
            setup.For<ICurrencyConversionHelper>().Use<CurrencyConversionHelper>();
            setup.For<IRepositoryFactory>().Use<RepositoryFactory>();
            setup.For<IFixedCostRepository>().Use<FixedCostRepository>();
            setup.For<ISaleEventRepository>().Use<SaleEventRepository>();
            setup.For<IRecipeRepository>().Use<RecipeRepository>();
            setup.For<IOrderWeightCalculator>().Use<OrderWeightCalculator>();
            setup.For<KitsImpExpModule>().Use<KitsImpExpModule>();
            setup.For<EshopProductMappingsImpExpModule>().Use<EshopProductMappingsImpExpModule>();
            setup.For<MaterialReportingGroupsImpExpModule>().Use<MaterialReportingGroupsImpExpModule>();
            setup.For<AbandonedBatchRulesImpExp>().Use<AbandonedBatchRulesImpExp>();
            setup.For<IUserNickProvider>().Import.FromFactory(sl => sl.Get<IUserRepository>());
        }
    }
}
