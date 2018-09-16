namespace Elsa.Jobs.Common
{
    public interface IJobScheduler
    {
        JobSchedulerEntry Next();
    }
}
