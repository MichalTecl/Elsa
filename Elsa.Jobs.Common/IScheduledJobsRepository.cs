using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Automation;

namespace Elsa.Jobs.Common
{
    public interface IScheduledJobsRepository
    {
        IJobSchedule GetCurrentJob(int projectId);

        void MarkJobStarted(IJobSchedule job);

        void MarkJobSucceeded(IJobSchedule job);

        void MarkJobFailed(IJobSchedule job);
    }
}
