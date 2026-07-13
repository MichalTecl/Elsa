using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Automation;

namespace Elsa.Jobs.Common
{
    public interface IScheduledJobsRepository
    {
        IJobSchedule GetCurrentJob(int projectId);

        IJobExecutionLog MarkJobStarted(IJobSchedule job);

        void MarkJobSucceeded(IJobSchedule job);

        void MarkJobSucceeded(IJobSchedule job, IJobExecutionLog executionLog);

        void MarkJobFailed(IJobSchedule job);

        void MarkJobFailed(IJobSchedule job, IJobExecutionLog executionLog, string errorMessage);

        IEnumerable<IJobExecutionLog> GetExecutionLogs(int scheduledJobId, int maxCount);

        IEnumerable<IJobSchedule> GetCompleteScheduler();

        IJobSchedule GetJobByUid(string uid);
    }
}
