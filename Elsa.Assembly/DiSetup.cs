using Elsa.App.Commerce.Payments;
using Elsa.App.Commerce.Preview;
using Elsa.App.Crm;
using Elsa.App.OrdersPacking;
using Elsa.App.Shipment;
using Elsa.Apps.CommonData;
using Elsa.Apps.Inventory;
using Elsa.Apps.ScheduledJobs;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.UserRightsInfrastructure;
using Elsa.Core.Entities.Commerce;
using Elsa.Integration.Erp.Elerp;
using Elsa.Integration.Erp.Fler;
using Elsa.Integration.Erp.Flox;
using Elsa.Integration.Geocoding.OpenStreetMap.Model;
using Elsa.Integration.PaymentSystems.Common;
using Elsa.Integration.PaymentSystems.Fio;
using Elsa.Integration.ShipmentProviders.Zasilkovna;
using Elsa.Jobs.Common;
using Elsa.Jobs.GeocodeAddresses;
using Elsa.Jobs.ImportOrders;
using Elsa.Jobs.LoadCurrencyRates;
using Elsa.Jobs.PaymentPairing;
using Elsa.Jobs.PrefillCalender;
using Elsa.Jobs.SetPaidStatus;
using Elsa.Jobs.SyncErpCustomers;
using Elsa.Users;

using Robowire;

namespace Elsa.Assembly
{
    public static class DiSetup
    {
        public static IContainer GetContainer()
        {
            var container = new Container();
            SetupContainer(container);
            return container;
        }

        public static void SetupContainer(IContainer container)
        {
            container.Setup(s => s.For<IWebSession>().Use<UserWebSession>());
            container.Setup(s => s.For<ISession>().Import.FromFactory(l => l.Get<IWebSession>()));
            container.Setup(s => s.Collect<IStartupJob>());
            container.Setup(s => s.For<SyncUserRightsJob>().Use<SyncUserRightsJob>());
            container.Setup(
                s =>
                {
                    s.ScanAssembly(typeof(FloxClient).Assembly);
                    s.ScanAssembly(typeof(FlerClient).Assembly);
                    s.ScanAssembly(typeof(FioClient).Assembly);
                    s.ScanAssembly(typeof(ScheduledJobsController).Assembly);
                    s.ScanAssembly(typeof(IScheduledJobsRepository).Assembly);
                    s.ScanAssembly(typeof(ImportOrdersJob).Assembly);
                    s.ScanAssembly(typeof(PairPaymentsJob).Assembly);
                    s.ScanAssembly(typeof(PrefillCalendarJob).Assembly);
                    s.ScanAssembly(typeof(SetOrdersPaid).Assembly);
                    s.ScanAssembly(typeof(IPaymentSystemClientFactory).Assembly);
                    s.ScanAssembly(typeof(VirtualProductsController).Assembly);
                    s.ScanAssembly(typeof(PreviewController).Assembly);
                    s.ScanAssembly(typeof(IPurchaseOrderRepository).Assembly);
                    s.ScanAssembly(typeof(PaymentsPairingController).Assembly);
                    s.ScanAssembly(typeof(ZasilkovnaClient).Assembly);
                    s.ScanAssembly(typeof(ShipmentController).Assembly);
                    s.ScanAssembly(typeof(PackingController).Assembly);
                    s.ScanAssembly(typeof(ElerpClient).Assembly);
                    s.ScanAssembly(typeof(CustomersSyncJob).Assembly);
                    s.ScanAssembly(typeof(CustomersController).Assembly);
                    s.ScanAssembly(typeof(ImportRatesJob).Assembly);
                    s.ScanAssembly(typeof(CommonRegistry).Assembly);
                    s.ScanAssembly(typeof(SuppliersAutoController).Assembly);
                });

            ElsaDbInstaller.Initialize(container);
        }
    }
}
