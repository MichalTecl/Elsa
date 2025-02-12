using System.Diagnostics;

using Elsa.App.Commerce.Payments;
using Elsa.App.Commerce.Preview;
using Elsa.App.CommonReports;
using Elsa.App.Crm;
using Elsa.App.ImportExport;
using Elsa.App.Inspector;
using Elsa.App.MaterialLevels;
using Elsa.App.OrdersPacking;
using Elsa.App.Profile;
using Elsa.App.PublicFiles;
using Elsa.App.SaleEvents;
using Elsa.App.Shipment;
using Elsa.Apps.CommonData;
using Elsa.Apps.EshopMapping;
using Elsa.Apps.Inventory;
using Elsa.Apps.InvoiceForms;
using Elsa.Apps.Invoices;
using Elsa.Apps.ProductionPlanner;
using Elsa.Apps.ProductionService;
using Elsa.Apps.Reporting;
using Elsa.Apps.ScheduledJobs;
using Elsa.Commerce.Core;
using Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration;
using Elsa.Common;
using Elsa.Common.EntityComments;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Common.XTable;
using Elsa.Core.Entities.Commerce;
using Elsa.Integration.ChatGpt;
using Elsa.Integration.Crm.Raynet;
using Elsa.Integration.Erp.Flox;
using Elsa.Integration.PaymentSystems.Common;
using Elsa.Integration.PaymentSystems.Fio;
using Elsa.Integration.ShipmentProviders.Zasilkovna;
using Elsa.Invoicing.Core.Contract;
using Elsa.Jobs.AutomaticQueries;
using Elsa.Jobs.BuildStoresMap;
using Elsa.Jobs.Common;
using Elsa.Jobs.Common.EntityChangeProcessing;
using Elsa.Jobs.DbBackup;
using Elsa.Jobs.ExternalSystemsDataPush;
using Elsa.Jobs.FinancialReportsGeneration;
using Elsa.Jobs.GeocodeAddresses;
using Elsa.Jobs.ImportOrders;
using Elsa.Jobs.LoadCurrencyRates;
using Elsa.Jobs.OrderDataValidation;
using Elsa.Jobs.PaymentPairing;
using Elsa.Jobs.PrefillCalender;
using Elsa.Jobs.SetPaidStatus;
using Elsa.Jobs.SyncErpCustomers;
using Elsa.Smtp.Core;
using Elsa.Users;
using Robowire;

namespace Elsa.Assembly
{
    public static class DiSetup
    {
        public static IContainer GetContainer(ILogWriter logWriter)
        {
            var container = new Container();
            SetupContainer(container, logWriter);
            return container;
        }

        public static void SetupContainer(IContainer container, ILogWriter logWriter)
        {
            Debug.WriteLine("Setting up the container");

            container.Setup(s => s.For<IWebSession>().Use<UserWebSession>());
            container.Setup(s => s.For<ISession>().Import.FromFactory(l => l.Get<IWebSession>()));
            container.Setup(s => s.Collect<IStartupJob>());
            
            container.Setup(
                s =>
                {
                    s.ScanAssembly(typeof(ImportExportRegistry).Assembly);
                    s.ScanAssembly(typeof(IInvoiceFormGeneratorFactory).Assembly);
                    s.ScanAssembly(typeof(FloxClient).Assembly);
                    s.ScanAssembly(typeof(FioClient).Assembly);
                    s.ScanAssembly(typeof(ScheduledJobsController).Assembly);
                    s.ScanAssembly(typeof(IScheduledJobsRepository).Assembly);
                    s.ScanAssembly(typeof(ImportOrdersJob).Assembly);
                    s.ScanAssembly(typeof(PairPaymentsJob).Assembly);
                    s.ScanAssembly(typeof(PrefillCalendarJob).Assembly);
                    s.ScanAssembly(typeof(SetOrdersPaid).Assembly);
                    s.ScanAssembly(typeof(IPaymentSystemClientFactory).Assembly);
                    s.ScanAssembly(typeof(InventoryAppRegistry).Assembly);
                    s.ScanAssembly(typeof(PreviewController).Assembly);
                    s.ScanAssembly(typeof(IPurchaseOrderRepository).Assembly);
                    s.ScanAssembly(typeof(PaymentsPairingController).Assembly);
                    s.ScanAssembly(typeof(ZasilkovnaClient).Assembly);
                    s.ScanAssembly(typeof(ShipmentController).Assembly);
                    s.ScanAssembly(typeof(OrdersPackingRegistry).Assembly);
                    s.ScanAssembly(typeof(CustomersSyncJob).Assembly);
                    s.ScanAssembly(typeof(CustomersController).Assembly);
                    s.ScanAssembly(typeof(ImportRatesJob).Assembly);
                    s.ScanAssembly(typeof(CommonRegistry).Assembly);
                    s.ScanAssembly(typeof(SuppliersAutoController).Assembly);
                    s.ScanAssembly(typeof(InvoiceFormsController).Assembly);
                    s.ScanAssembly(typeof(XTableController).Assembly);
                    s.ScanAssembly(typeof(InvoiceFormsGenerationRunner).Assembly);
                    s.ScanAssembly(typeof(InvoiceFilesController).Assembly);
                    s.ScanAssembly(typeof(SaleEventsController).Assembly);
                    s.ScanAssembly(typeof(ProductionServiceController).Assembly);
                    s.ScanAssembly(typeof(CommonReportsController).Assembly);
                    s.ScanAssembly(typeof(DbBackupJob).Assembly);
                    s.ScanAssembly(typeof(InspectorRegistry).Assembly);
                    s.ScanAssembly(typeof(UserRightsRegistry).Assembly);
                    s.ScanAssembly(typeof(SmtpRegistry).Assembly);
                    s.ScanAssembly(typeof(MaterialLevelsRegistry).Assembly);
                    s.ScanAssembly(typeof(FinDataGenerationRegistry).Assembly);
                    s.ScanAssembly(typeof(AutoQueriesRegistry).Assembly);
                    s.ScanAssembly(typeof(ProfileController).Assembly);
                    s.ScanAssembly(typeof(ReportingRegistry).Assembly);
                    s.ScanAssembly(typeof(DataPushRegistry).Assembly);
                    s.ScanAssembly(typeof(EntityChangeProcessingRegistry).Assembly);
                    s.ScanAssembly(typeof(RaynetClientRegistry).Assembly);
                    s.ScanAssembly(typeof(ProductionPlannerRegistry).Assembly);
                    s.ScanAssembly(typeof(StoreMapRegistry).Assembly);
                    s.ScanAssembly(typeof(PublicFilesRegistry).Assembly);
                    s.ScanAssembly(typeof(OrderDataValidationJobRegistry).Assembly);
                    s.ScanAssembly(typeof(ChatGptRegistry).Assembly);
                    s.ScanAssembly(typeof(EntityCommentsRegistry).Assembly);
                    s.ScanAssembly(typeof(EshopMappingRegistry).Assembly);

                    s.For<ILogWriter>().ImportObject.Existing(logWriter);

                    s.Collect<IStartupJob>();
                });

            Debug.WriteLine("Container set up");

            Debug.WriteLine("Initializing ElsaDb");
            ElsaDbInstaller.Initialize(container);
            Debug.WriteLine("ElsaDb initialized");

            TlsSetter.Setup();
        }
    }    
}
