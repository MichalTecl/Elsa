using Elsa.Core.Entities.Commerce.Automation;

namespace Elsa.Jobs.Common
{
    public interface IJobExecutor
    {
        void LaunchJob(IJobSchedule jobEntry);
    }
}
