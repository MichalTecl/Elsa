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
        private const int MAX_EXECUTION_LOGS = 100;

        private readonly IScheduledJobsRepository m_jobsRepository;
        private readonly IJobExecutor m_executor;
        private readonly ICache m_cache;
        private readonly ISession m_session;
        private readonly ILog m_log;

        public ScheduledJobsController(IWebSession webSession, ILog log, IScheduledJobsRepository jobsRepository, IJobExecutor executor, ICache cache, ISession session)
            : base(webSession, log)
        {
            m_jobsRepository = jobsRepository;
            m_executor = executor;
            m_cache = cache;
            m_session = session;
            m_log = log;
        }

        [DoNotLog]
        public IEnumerable<ScheduledJobStatus> GetStatus()
        {
            var cacheKey = GetStatusCacheKey();
            var currentVersion = GetStatusVersion();
            var cachedStatus = m_cache.ReadThrough(
                cacheKey,
                TimeSpan.FromHours(1),
                () => CreateCachedStatus(currentVersion));

            if (cachedStatus.Version != currentVersion)
            {
                m_cache.Remove(cacheKey);
                cachedStatus = m_cache.ReadThrough(
                    cacheKey,
                    TimeSpan.FromHours(1),
                    () => CreateCachedStatus(currentVersion));
            }

            return cachedStatus.Statuses;
        }

        [DoNotLog]
        public bool CheckJobsHeartbeat() 
        {
            try
            {
                var lastTimeStamp = long.Parse(SharedFilesUtil.GetSharedValue("JobHeartbeat", DateTime.MinValue.Ticks.ToString()));
                var lastDt = new DateTime(lastTimeStamp);

                return (DateTime.Now - lastDt).TotalMinutes < 60;
            }
            catch (Exception ex)
            {
                m_log.Error("scheduledJobsController.CheckJobsHeartbeat failed", ex);
                return false;
            }            
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

        private CachedStatus CreateCachedStatus(string version)
        {
            var scheduler = m_jobsRepository.GetCompleteScheduler().OrderBy(s => s.LoopLaunchPriority).ToList();
            return new CachedStatus(version, CreateStatuses(scheduler).ToList());
        }

        private IEnumerable<ScheduledJobStatus> CreateStatuses(IList<IJobSchedule> scheduler)
        {
            var result = new List<ScheduledJobStatus>(scheduler.Count);

            foreach (var schedule in scheduler)
            {
                result.Add(new ScheduledJobStatus
                {
                    ScheduleId = schedule.Id,
                    Name = schedule.ScheduledJob.Name,
                    LastRun = schedule.LastStartDt == null
                        ? "Nikdy"
                        : DateUtil.FormatDateWithAgo(schedule.LastStartDt ?? schedule.LastEndDt.Value, true),
                    CanBeStarted = schedule.CanBeStartedManually && !IsRunning(schedule),
                    Executions = GetExecutionStatuses(schedule)
                });
            }

            return result;
        }

        private string GetStatusCacheKey()
        {
            return $"jobsStat_{m_session.Project.Id}";
        }

        private string GetStatusVersion()
        {
            try
            {
                return ScheduledJobsStatusVersion.Get(m_session.Project.Id);
            }
            catch (Exception ex)
            {
                m_log.Error("Cannot read scheduled jobs status version", ex);
                return string.Empty;
            }
        }

        private IEnumerable<JobExecutionStatus> GetExecutionStatuses(IJobSchedule schedule)
        {
            return m_jobsRepository.GetExecutionLogs(schedule.ScheduledJobId, MAX_EXECUTION_LOGS)
                .Reverse()
                .Select(ToExecutionStatus)
                .ToList();
        }

        private static JobExecutionStatus ToExecutionStatus(IJobExecutionLog execution)
        {
            var statusClass = execution.EndDt == null
                ? "jobRunRunning"
                : string.IsNullOrWhiteSpace(execution.ErrorMessage)
                    ? "jobRunSucceeded"
                    : "jobRunFailed";

            var tooltip = $"Spuštěno: {execution.StartDt:dd.MM.yyyy HH:mm:ss}\n" +
                          $"Spustil: {execution.StartUser.EMail}\n" +
                          $"Doba běhu: {GetDurationText(execution)}";

            if (!string.IsNullOrWhiteSpace(execution.ErrorMessage))
            {
                tooltip += $"\nChyba: {execution.ErrorMessage}";
            }

            return new JobExecutionStatus
            {
                Id = execution.Id,
                StatusClass = statusClass,
                Tooltip = tooltip
            };
        }

        private static string GetDurationText(IJobExecutionLog execution)
        {
            if (execution.EndDt == null)
            {
                return "Probíhá";
            }

            return FormatDuration(execution.EndDt.Value - execution.StartDt);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours} h {duration.Minutes} min {duration.Seconds} s";
            }

            if (duration.TotalMinutes >= 1)
            {
                return $"{duration.Minutes} min {duration.Seconds} s";
            }

            return $"{Math.Max(0, (int)duration.TotalSeconds)} s";
        }

        private bool IsRunning(IJobSchedule sche)
        {
            if (sche.LastStartDt == null)
            {
                return false;
            }

            return (sche.LastStartDt.Value < DateTime.Now) && (sche.LastEndDt == null);
        }

        private sealed class CachedStatus
        {
            public CachedStatus(string version, IEnumerable<ScheduledJobStatus> statuses)
            {
                Version = version;
                Statuses = statuses;
            }

            public string Version { get; }

            public IEnumerable<ScheduledJobStatus> Statuses { get; }
        }

    }
}
