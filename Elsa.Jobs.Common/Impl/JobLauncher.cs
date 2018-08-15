using System;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Automation;

using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.Common.Impl
{
    public class JobLauncher : IJobExecutor
    {
        private readonly IDatabase m_database;

        private readonly IServiceLocator m_serviceLocator;

        private readonly ISession m_session;

        public JobLauncher(IDatabase database, IServiceLocator serviceLocator, ISession session)
        {
            m_database = database;
            m_serviceLocator = serviceLocator;
            m_session = session;
        }

        public void LaunchJob(IScheduledJob jobEntry)
        {
            var jobExecutable = m_serviceLocator.InstantiateNow<IExecutableJob>(jobEntry.ModuleClass);

            var historyRecord = m_database.New<IJobExecutionHistory>();
            historyRecord.ScheduledJobId = jobEntry.Id;
            historyRecord.ExecUserId = m_session.User.Id;
            historyRecord.LastStartDt = DateTime.Now;

            m_database.Save(historyRecord);

            try
            {
                jobExecutable.Run(jobEntry.CustomData);
            }
            catch (Exception ex)
            {
                //using(var connection = )

                var msg = ex.ToString();
                if (msg.Length > 256)
                {
                    msg = msg.Substring(0, 256);
                }

                historyRecord.ErrorMessage = msg;
                historyRecord.LastEndDt = DateTime.Now;
                
                m_database.Save(historyRecord);

                throw;
            }
            finally
            {
                historyRecord.LastEndDt = DateTime.Now;
                m_database.Save(historyRecord);
            }
        }
    }
}
