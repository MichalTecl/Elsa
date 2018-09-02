using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Automation;

using Robowire.RobOrm.Core;

namespace Elsa.Jobs.Common.Impl
{
    public class ScheduledJobsRepository : IScheduledJobsRepository
    {
        private readonly IDatabase m_database;

        public ScheduledJobsRepository(IDatabase database)
        {
            m_database = database;
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

        public void MarkJobStarted(IJobSchedule job)
        {
            job.LastEndDt = null;
            job.LastStartDt = DateTime.Now;
            
            m_database.Save(job);
        }

        public void MarkJobSucceeded(IJobSchedule job)
        {
            job.LastEndDt = DateTime.Now;
            job.LastRunFailed = false;
            m_database.Save(job);
        }

        public void MarkJobFailed(IJobSchedule job)
        {
            job.LastEndDt = DateTime.Now;
            job.LastRunFailed = true;
            m_database.Save(job);
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
