namespace Schedoo.Core
{
    public interface IJobContext : ITimeConditions
    {
        bool DidntRunMoreThan(int hours, int minutes, int seconds);

        bool LastTimeFailed();

        bool LastTimeSucceded();

        bool IsNowRunning();
    }
}
