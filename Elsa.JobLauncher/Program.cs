using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Jobs.Common;

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
