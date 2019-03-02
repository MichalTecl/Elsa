using System;
using System.Collections.Generic;

using Elsa.Jobs.Common;

using Schedoo.Core;

namespace Elsa.JobLauncher.Scheduler
{
    public class ElsaJobRepo : IDataRepository
    {
        private readonly IScheduledJobsRepository m_scheduledJobs;

        public ElsaJobRepo(IScheduledJobsRepository scheduledJobs)
        {
            m_scheduledJobs = scheduledJobs;
        }

        public IEnumerable<IJob> AllJobs => JobDefinitions.All;

        public DateTime Now => DateTime.Now;

        public void OnJobStart(IJob job)
        {
            m_scheduledJobs.MarkJobStarted(m_scheduledJobs.GetJobByUid(job.Uid));
        }

        public void OnJobSucceeded(IJob job)
        {
            m_scheduledJobs.MarkJobSucceeded(m_scheduledJobs.GetJobByUid(job.Uid));
        }

        public void OnJobFailed(IJob job, Exception ex)
        {
            m_scheduledJobs.MarkJobFailed(m_scheduledJobs.GetJobByUid(job.Uid));
        }

        public DateTime? GetLastJobStarted(IJob job)
        {
            var jobEntity = m_scheduledJobs.GetJobByUid(job.Uid);
            return jobEntity?.LastStartDt;
        }

        public DateTime? GetLastJobSucceeded(IJob job)
        {
            var jobEntity = m_scheduledJobs.GetJobByUid(job.Uid);
            if (jobEntity == null)
            {
                return null;
            }

            if (jobEntity.LastRunFailed == true)
            {
                return null;
            }

            return jobEntity.LastEndDt;
        }

        public DateTime? GetLastJobFailed(IJob job)
        {
            var jobEntity = m_scheduledJobs.GetJobByUid(job.Uid);
            if (jobEntity == null)
            {
                return null;
            }

            if (jobEntity.LastRunFailed == false)
            {
                return null;
            }

            return jobEntity.LastEndDt;
        }
    }
}
