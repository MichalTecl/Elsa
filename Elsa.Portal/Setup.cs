
using Elsa.App.Commerce.Payments;
using Elsa.App.Commerce.Preview;
using Elsa.App.OrdersPacking;
using Elsa.App.Shipment;
using Elsa.Apps.Inventory;
using Elsa.Apps.ScheduledJobs;
using Elsa.Common;
using Elsa.Users;

using Robowire;
using Elsa.Commerce.Core;
using Elsa.Integration.Erp.Elerp;
using Elsa.Integration.Erp.Fler;
using Elsa.Integration.Erp.Flox;
using Elsa.Integration.PaymentSystems.Common;
using Elsa.Integration.PaymentSystems.Fio;
using Elsa.Integration.ShipmentProviders.Zasilkovna;
using Elsa.Jobs.Common;
using Elsa.Jobs.ImportOrders;
using Elsa.Jobs.PaymentPairing;
using Elsa.Jobs.PrefillCalender;
using Elsa.Jobs.SetPaidStatus;

namespace Elsa.Portal
{
    public static class Setup
    {
        public static void SetupContainer(IContainer container)
        {
            CommonRegistry.SetupContainer(container);

            container.Setup(s => s.For<IWebSession>().Use<UserWebSession>());
            container.Setup(s => s.For<ISession>().Import.FromFactory(l => l.Get<IWebSession>()));
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
                    });
        }
    }
}