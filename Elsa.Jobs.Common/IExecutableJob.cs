namespace Elsa.Jobs.Common
{
    public interface IExecutableJob
    {
        void Run(string customDataJson);
    }
}
