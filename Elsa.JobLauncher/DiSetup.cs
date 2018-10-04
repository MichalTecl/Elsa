using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Core.Entities.Commerce;
using Elsa.Integration.Erp.Fler;
using Elsa.Integration.Erp.Flox;
using Elsa.Integration.Geocoding.OpenStreetMap.Model;
using Elsa.Integration.PaymentSystems.Common;
using Elsa.Integration.PaymentSystems.Fio;
using Elsa.Jobs.Common;
using Elsa.Jobs.GeocodeAddresses;
using Elsa.Jobs.PaymentPairing;
using Elsa.Jobs.PrefillCalender;
using Elsa.Jobs.SetPaidStatus;

using Robowire;

namespace Elsa.JobLauncher
{
    public static class DiSetup
    {
        public static IContainer GetContainer()
        {
            var container = new Container();

            ElsaDbInstaller.Initialize(container);

            container.Setup(s => s.For<ISession>().Use<JobSession>());
            
            CommonRegistry.SetupContainer(container);

            container.Setup(s => s.ScanAssembly(typeof(IScheduledJobsRepository).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(IErpClientFactory).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(FloxClient).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(FlerClient).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(IPaymentSystemClientFactory).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(FioClient).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(PairPaymentsJob).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(PrefillCalendarJob).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(SetOrdersPaid).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(OsmGeoResponse).Assembly));
            container.Setup(s => s.ScanAssembly(typeof(LoadGeo).Assembly));
            return container;
        }
    }
}
