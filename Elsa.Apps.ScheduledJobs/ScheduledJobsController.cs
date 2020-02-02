using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Elsa.Apps.ScheduledJobs.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Automation;
using Elsa.Jobs.Common;

using Robowire.RoboApi;

namespace Elsa.Apps.ScheduledJobs
{
    [Controller("scheduledJobs")]
    public class ScheduledJobsController : ElsaControllerBase
    {
        private readonly IScheduledJobsRepository m_jobsRepository;
        private readonly IJobExecutor m_executor;
        private readonly ICache m_cache;
        private readonly ISession m_session;

        public ScheduledJobsController(IWebSession webSession, ILog log, IScheduledJobsRepository jobsRepository, IJobExecutor executor, ICache cache, ISession session)
            : base(webSession, log)
        {
            m_jobsRepository = jobsRepository;
            m_executor = executor;
            m_cache = cache;
            m_session = session;
        }

        [DoNotLog]
        public IEnumerable<ScheduledJobStatus> GetStatus()
        {
            return m_cache.ReadThrough($"jobsStat_{m_session.Project.Id}",
                TimeSpan.FromSeconds(20),
                () =>
                {
                    var scheduler = m_jobsRepository.GetCompleteScheduler().OrderBy(s => s.LoopLaunchPriority).ToList();

                    var result = new List<ScheduledJobStatus>(scheduler.Count);

                    foreach (var sch in scheduler)
                    {
                        result.Add(new ScheduledJobStatus()
                        {
                            ScheduleId = sch.Id,
                            Name = sch.ScheduledJob.Name,
                            LastRun =
                                sch.LastStartDt == null
                                    ? "Nikdy"
                                    : DateUtil.FormatDateWithAgo(sch.LastStartDt ?? sch.LastEndDt.Value, true),
                            CanBeStarted = sch.CanBeStartedManually && !IsRunning(sch),
                            CurrentStatus = GetJobStatus(sch),
                            StartMode = GetStartMode(sch)
                        });
                    }

                    return result;
                });
        }

        public IEnumerable<ScheduledJobStatus> Launch(int scheduleId)
        {
            var schedule = m_jobsRepository.GetCompleteScheduler().FirstOrDefault(s => s.Id == scheduleId);
            if (schedule == null)
            {
                throw new InvalidOperationException("Invalid schedule");
            }

            Task.Run(() => m_executor.LaunchJob(schedule));

            return GetStatus();
        }

        private string GetStartMode(IJobSchedule sch)
        {
            if (sch.Active)
            {
                if (sch.CanBeStartedManually)
                {
                    return "Automaticky nebo ručně";
                }
                else
                {
                    return "Pouze automaticky";
                }
            }
            else
            {
                return "Pouze ručně";
            }
        }

        private string GetJobStatus(IJobSchedule sch)
        {
            if (IsRunning(sch))
            {
                return "Probíhá";
            }

            return (sch.LastRunFailed ?? false) ? "Selhal" : "OK";
        }

        private bool IsRunning(IJobSchedule sche)
        {
            if (sche.LastStartDt == null)
            {
                return false;
            }

            return (sche.LastStartDt.Value < DateTime.Now) && (sche.LastEndDt == null);
        }

    }
}
