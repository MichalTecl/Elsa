using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Automation;

using Robowire.RobOrm.Core;

namespace Elsa.Jobs.Common.Impl
{
    public class ScheduledJobsRepository : IScheduledJobsRepository
    {
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ILog m_log;

        public ScheduledJobsRepository(IDatabase database, ISession session, ILog log)
        {
            m_database = database;
            m_session = session;
            m_log = log;
        }

        public IJobSchedule GetCurrentJob(int projectId)
        {
            //0. update jobs with missing EndDt
            var misalignedJobs =
                m_database.SelectFrom<IJobSchedule>().Where(i => i.LastStartDt != null && i.LastEndDt == null).Execute();

            foreach (var misaljob in misalignedJobs)
            {
                misaljob.LastEndDt = misaljob.LastStartDt;
                misaljob.LastRunFailed = true;
                m_database.Save(misaljob);
            }

            var interruptedExecutions = m_database.SelectFrom<IJobExecutionLog>()
                .Join(e => e.ScheduledJob)
                .Where(e => e.ScheduledJob.ProjectId == projectId && e.EndDt == null)
                .Execute();

            foreach (var execution in interruptedExecutions)
            {
                execution.EndDt = execution.StartDt;
                execution.ErrorMessage = "Běh úlohy nebyl korektně ukončen.";
                m_database.Save(execution);
            }
            
            var schedule =
                m_database.SelectFrom<IJobSchedule>()
                    .Join(s => s.ScheduledJob)
                    .Where(s => s.ProjectId == projectId && s.ScheduledJob.ProjectId == projectId)
                    .Where(s => s.Active)
                    .Execute().ToList();
            

            //1. find job which should be executed in current sequence
            var sequenceDependants = schedule.Where(i => i.RunAfterId != null).ToList();
            foreach (var dep in sequenceDependants)
            {
                var predecessor =
                    schedule.FirstOrDefault(i =>
                            i.Id == dep.RunAfterId
                            && (i.LastEndDt ?? DateTime.MinValue) < (dep.LastStartDt ?? DateTime.MinValue)
                            && i.LastRunFailed != true);
                if (predecessor != null)
                {
                    return dep;
                }
            }

            //2. find job which should be executed by time
            var timedJobs = schedule.Where(i => !string.IsNullOrWhiteSpace(i.StartTimeHhMm));
            foreach (var timedJob in timedJobs)
            {
                var mustStartAfter = GetLastHhMmTime(timedJob.StartTimeHhMm);

                var lastTime = timedJob.LastEndDt ?? timedJob.LastStartDt ?? DateTime.MinValue;
                if (lastTime < mustStartAfter)
                {
                    return timedJob;
                }
            }
            
            //3. find failed jobs to retry
            var failedJob = schedule.Where(s => s.LastRunFailed == true && IsRetryTime(s))
                    .OrderBy(j => j.LastStartDt)
                    .FirstOrDefault();

            if (failedJob != null)
            {
                return failedJob;
            }

            var justRunJob =
                schedule.Where(j => j.LoopLaunchPriority != null)
                    .Where(IsRetryTime)
                    .OrderBy(i => i.LoopLaunchPriority ?? 0)
                    .FirstOrDefault();

            return justRunJob;
        }

        public IJobExecutionLog MarkJobStarted(IJobSchedule job)
        {
            m_log.Info($"MarkJobStarted called for job.Uid = {job?.Uid}");

            if (string.IsNullOrEmpty(job?.Uid))
                return null;

            if (job.LastStartDt != null && job.LastEndDt == null)
            {
                var runningExecution = FindRunningExecution(job.ScheduledJobId);
                if (runningExecution != null)
                {
                    return runningExecution;
                }
            }

            var startDt = DateTime.Now;
            job.LastEndDt = null;
            job.LastStartDt = startDt;
            m_database.Save(job);

            var executionLog = m_database.New<IJobExecutionLog>();
            executionLog.ScheduledJobId = job.ScheduledJobId;
            executionLog.StartUserId = m_session.User.Id;
            executionLog.StartDt = startDt;
            m_database.Save(executionLog);

            m_log.Info($"MarkJobStarted OK {job?.Uid}");
            return executionLog;
        }

        public void MarkJobSucceeded(IJobSchedule job)
        {
            var executionLog = FindRunningExecution(job?.ScheduledJobId);
            if (executionLog != null)
            {
                MarkJobSucceeded(job, executionLog);
            }
        }

        public void MarkJobSucceeded(IJobSchedule job, IJobExecutionLog executionLog)
        {
            m_log.Info($"MarkJobSucceeded called for job.Uid = {job?.Uid}");

            if (string.IsNullOrEmpty(job?.Uid))
                return;

            job.LastEndDt = DateTime.Now;
            job.LastRunFailed = false;
            m_database.Save(job);

            if (executionLog != null)
            {
                executionLog.EndDt = job.LastEndDt;
                m_database.Save(executionLog);
            }

            m_log.Info($"MarkJobSucceeded OK {job?.Uid}");
        }

        public void MarkJobFailed(IJobSchedule job)
        {
            var executionLog = FindRunningExecution(job?.ScheduledJobId);
            if (executionLog != null)
            {
                MarkJobFailed(job, executionLog, "Běh úlohy selhal.");
            }
        }

        public void MarkJobFailed(IJobSchedule job, IJobExecutionLog executionLog, string errorMessage)
        {
            m_log.Info($"MarkJobFailed called for job.Uid = {job?.Uid}");

            if (string.IsNullOrEmpty(job?.Uid))
                return;

            job.LastEndDt = DateTime.Now;
            job.LastRunFailed = true;
            m_database.Save(job);

            if (executionLog != null)
            {
                executionLog.EndDt = job.LastEndDt;
                executionLog.ErrorMessage = errorMessage;
                m_database.Save(executionLog);
            }

            m_log.Info($"MarkJobFailed OK {job?.Uid}");
        }

        public IEnumerable<IJobExecutionLog> GetExecutionLogs(int scheduledJobId, int maxCount)
        {
            return m_database.SelectFrom<IJobExecutionLog>()
                .Join(e => e.StartUser)
                .Where(e => e.ScheduledJobId == scheduledJobId)
                .OrderByDesc(e => e.StartDt)
                .Take(maxCount)
                .Execute();
        }

        private IJobExecutionLog FindRunningExecution(int? scheduledJobId)
        {
            if (scheduledJobId == null)
            {
                return null;
            }

            return m_database.SelectFrom<IJobExecutionLog>()
                .Where(e => e.ScheduledJobId == scheduledJobId.Value && e.EndDt == null)
                .OrderByDesc(e => e.StartDt)
                .Take(1)
                .Execute()
                .FirstOrDefault();
        }

        public IEnumerable<IJobSchedule> GetCompleteScheduler()
        {
            var projectId = m_session.Project.Id;
            return
                m_database.SelectFrom<IJobSchedule>()
                    .Join(s => s.ScheduledJob)
                    .Where(j => j.ProjectId == projectId)
                    .Execute();
        }

        public IJobSchedule GetJobByUid(string uid)
        {
            var schedule = GetCompleteScheduler().FirstOrDefault(j => j.Uid == uid);
            if (schedule == null)
            {
                m_log.Info($"Schedule for job \"{uid}\" not found");
            }

            return schedule;
        }

        private static DateTime GetLastHhMmTime(string hhMm)
        {
            int hour;
            int minute;

            if (   (hhMm.Length != 4)
                || !int.TryParse(hhMm.Substring(0, 2), out hour)
                || !int.TryParse(hhMm.Substring(2, 2), out minute)
                )
            {
                throw new InvalidOperationException($"\"{hhMm}\" is not valid HHMM time expression");
            }

            var today = DateTime.Now.Date.AddHours(hour).AddMinutes(minute);

            if (today > DateTime.Now)
            {
                today = today.AddDays(-1);
            }

            return today;
        }

        private static bool IsRetryTime(IJobSchedule schedule)
        {
            if (schedule.RetryMinutes == null)
            {
                return true;
            }

            var retryTime = schedule.LastStartDt ?? DateTime.MinValue;

            retryTime = retryTime.AddMinutes(schedule.RetryMinutes ?? 0);

            return retryTime <= DateTime.Now;
        }
    }
}
