using System;
using System.Linq;

using Elsa.Common.Logging;
using Elsa.Jobs.Common;

using Robowire;

using Schedoo.Core;

namespace Elsa.JobLauncher.Scheduler
{
    public class ElsaJobsScheduler : SchedulerBase
    {
        private readonly ILog m_log;
        private readonly IScheduledJobsRepository m_jobsRepository;
        private readonly IJobExecutor m_executor;

        public ElsaJobsScheduler(IDataRepository dataRepository, ILog log, IScheduledJobsRepository jobsRepository, IJobExecutor executor)
            : base(dataRepository, TimeSpan.FromSeconds(10))
        {
            m_log = log;
            m_jobsRepository = jobsRepository;
            m_executor = executor;
        }

        protected override void StartJob(IJob job)
        {
            var elsaJob = job as ElsaJob;
            if (string.IsNullOrWhiteSpace(elsaJob?.Uid))
            {
                throw new InvalidOperationException("ElsaJob expected");
            }

            var jobEntity = m_jobsRepository.GetCompleteScheduler().FirstOrDefault(j => j.Uid == job.Uid);
            if (jobEntity == null)
            {
                throw new InvalidOperationException($"Invalid job Uid {elsaJob.Uid}");
            }
            
            m_executor.LaunchJob(jobEntity);
        }

        protected override void OnContextCreationFailed(IJob job, Exception ex)
        {
            m_log.Error($"Job context creation failed Job={job}", ex);
        }

        protected override void OnJobStart(IJob job)
        {
            m_log.Info($"Starting job {job}");
        }

        protected override void OnJobSucceeded(IJob job)
        {
            m_log.Info($"Job {job} finished ok");
        }

        protected override void WriteContextLog(IJobContext context, bool preconditonsEvaluationResult)
        {
            m_log.Info($"{context} (preconditionsEvalResult={preconditonsEvaluationResult})");
        }

        protected override void OnJobFailed(IJob job, Exception ex)
        {
            m_log.Error($"Job {job} failed", ex);
        }
    }
}
