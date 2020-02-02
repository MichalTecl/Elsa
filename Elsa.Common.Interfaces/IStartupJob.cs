namespace Elsa.Common.Interfaces
{
    public interface IStartupJob
    {
        bool IsExceptionFatal { get; }

        void Execute();
    }
}
