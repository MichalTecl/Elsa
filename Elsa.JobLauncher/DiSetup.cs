using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Core.Entities.Commerce;
using Elsa.Integration.Erp.Flox;
using Elsa.Jobs.Common;

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


            return container;
        }
    }
}
