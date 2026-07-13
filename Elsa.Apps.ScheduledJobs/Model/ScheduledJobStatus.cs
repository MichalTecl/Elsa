namespace Elsa.Apps.ScheduledJobs.Model
{
    public class ScheduledJobStatus
    {
        public int ScheduleId { get; set; }

        public string Name { get; set; }

        public System.Collections.Generic.IEnumerable<JobExecutionStatus> Executions { get; set; }

        public string LastRun { get; set; }

        public bool CanBeStarted { get; set; }
    }

    public class JobExecutionStatus
    {
        public int Id { get; set; }

        public string StatusClass { get; set; }

        public string Tooltip { get; set; }
    }
}
