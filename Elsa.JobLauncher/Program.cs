using System;
using System.Configuration;
using System.Linq;
using System.Threading;

using Elsa.Assembly;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.JobLauncher.Scheduler;

using Schedoo.Core;

namespace Elsa.JobLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Checking for another instances...");
            SingleInstanceBlock.EnsureSignleInstance("elsa_job_launcher");
            Console.WriteLine("Starting...");

            SharedFilesUtil.SetSharedValue("JobHeartbeat", DateTime.Now.Ticks.ToString());

            var container = DiSetup.GetContainer(new FileLogWriter("Jobs"));
            container.Setup(s => s.For<ISession>().Use<JobSession>());
            container.Setup(s => s.For<IDataRepository>().Use<ElsaJobRepo>());
            container.Setup(s => s.For<ElsaJobsScheduler>().Use<ElsaJobsScheduler>());
            
            var user = ConfigurationManager.AppSettings["robotLogin"];
            var password = ConfigurationManager.AppSettings["robotPassword"];
            
            Console.WriteLine("Creating container...");

            using (var locator = container.GetLocator())
            {
                Console.WriteLine("Container created");

                var session = (locator.Get<ISession>() as JobSession);

                if (session == null)
                {
                    throw new InvalidOperationException("Cannot instatiate session");
                }

                session.Login(user, password);

                Console.WriteLine("Authenticated, starting scheduler");

                var scheduler = locator.Get<ElsaJobsScheduler>();
                scheduler.Start(args.FirstOrDefault());
            }
            
        }
    }
}
