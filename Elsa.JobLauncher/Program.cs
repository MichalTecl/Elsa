using System.Threading;

namespace Elsa.JobLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = DiSetup.GetContainer();

            var manager = new JobsManager(container, "michal", "123123");

            while (true)
            {
                var wait = manager.Run();
                Thread.Sleep(wait);
            }
        }
    }
}
