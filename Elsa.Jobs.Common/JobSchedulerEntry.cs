namespace Elsa.Jobs.Common
{
    public sealed class JobSchedulerEntry
    {
        public JobSchedulerEntry(int nextJobId, int secondsDelay)
        {
            NextJobId = nextJobId;
            SecondsDelay = secondsDelay;
        }

        public int NextJobId { get; }

        public int SecondsDelay { get; }
    }
}
