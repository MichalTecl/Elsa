using System;
using System.Collections.Generic;
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

        public IScheduledJob GetCurrentJob(int projectId)
        {
            var now = DateTime.Now;
            var future = DateTime.Now.AddDays(10);

            var allJobs =
                m_database.SelectFrom<IScheduledJob>()
                    .Join(s => s.History)
                    .Where(j => j.ProjectId == projectId)
                    .Where(j => j.ActiveFrom < now)
                    .Where(j => (j.ActiveTo ?? future) > now)
                    .Execute().ToList();

            var job = allJobs.Where(j => !IsRunning(j) && GetNextRunTime(j) < DateTime.Now).OrderBy(j => j.SequencePriority).FirstOrDefault();
            return job;
        }

        private DateTime GetNextRunTime(IScheduledJob job)
        {
            var lastFinishDate = job.History.Max(h => h.LastEndDt) ?? DateTime.MinValue;

            return lastFinishDate.AddSeconds(job.SecondsInterval);
        }

        private bool IsRunning(IScheduledJob job)
        {
            return job.History.Any(h => h.LastEndDt == null);
        }
    }
}
