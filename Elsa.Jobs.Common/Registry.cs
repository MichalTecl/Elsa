
using Elsa.Jobs.Common.Impl;

using Robowire;

namespace Elsa.Jobs.Common
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IScheduledJobsRepository>().Use<ScheduledJobsRepository>();
            setup.For<IJobExecutor>().Use<JobLauncher>();
        }
    }
}
