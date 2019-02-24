using System.Threading;

using Elsa.Assembly;
using Elsa.Common;

namespace Elsa.JobLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = DiSetup.GetContainer();
            container.Setup(s => s.For<ISession>().Use<JobSession>());

            var manager = new JobsManager(container, "michal", "123123");

            while (true)
            {
                var wait = manager.Run();
                Thread.Sleep(wait);
            }
        }
    }
}
