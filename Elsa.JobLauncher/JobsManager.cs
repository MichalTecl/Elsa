using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Automation;
using Elsa.Jobs.Common;

using Robowire;

namespace Elsa.JobLauncher
{
    public class JobsManager
    {
        private readonly IContainer m_container;
        private readonly string m_user;
        private readonly string m_password;

        public JobsManager(IContainer container, string user, string password)
        {
            m_container = container;
            m_user = user;
            m_password = password;
        }

        public int Run()
        {
            using (var locator = m_container.GetLocator())
            {
                var session = (locator.Get<ISession>() as JobSession);

                if (session == null)
                {
                    throw new InvalidOperationException("Cannot instatiate session");
                }

                session.Login(m_user, m_password);

                var jobRepo = locator.Get<IScheduledJobsRepository>();
                var job = jobRepo.GetCurrentJob(session.Project.Id);

                if (job == null)
                {
                    return 30000;
                }

                var launcher = locator.Get<IJobExecutor>();

                try
                {
                    launcher.LaunchJob(job);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            return 1000;
        }
    }
}
