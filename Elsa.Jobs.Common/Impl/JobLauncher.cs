using System;

using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Automation;

using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.Common.Impl
{
    public class JobLauncher : IJobExecutor
    {
        private readonly IServiceLocator m_serviceLocator;
        private readonly IScheduledJobsRepository m_scheduledJobs;
        private readonly ILog m_log;

        public JobLauncher(IServiceLocator serviceLocator, IScheduledJobsRepository scheduledJobs, ILog log)
        {
            m_serviceLocator = serviceLocator;
            m_scheduledJobs = scheduledJobs;
            m_log = log;
        }

        public void LaunchJob(IJobSchedule jobEntry)
        {
            m_log.Info($"Vytvarim instanci {jobEntry.ScheduledJob.ModuleClass}");
            var jobExecutable = m_serviceLocator.InstantiateNow<IExecutableJob>(jobEntry.ScheduledJob.ModuleClass);


            m_scheduledJobs.MarkJobStarted(jobEntry);

            try
            {
                m_log.Info("Spoustim job");
                jobExecutable.Run(jobEntry.ScheduledJob.CustomData);

                m_log.Info("Job uspesne dokoncen");
                m_scheduledJobs.MarkJobSucceeded(jobEntry);
            }
            catch (Exception ex)
            {
                m_log.Error("Job selhal", ex);
                m_scheduledJobs.MarkJobFailed(jobEntry);
            }
        }
    }
}
