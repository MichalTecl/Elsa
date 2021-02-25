using System;
using System.Collections.Generic;
using System.Text;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.AutomaticQueries
{
    public class AutoProceduresJob
    {
        private readonly ILog m_log;
        private readonly IDatabase m_database;
        private readonly ISession m_session;

        public AutoProceduresJob(ILog log, IDatabase database, ISession session)
        {
            m_log = log;
            m_database = database;
            m_session = session;
        }

        public void Run()
        {
            m_log.Info($"Starting AutoProceduresJob");

            var procedures = GetProcedures();

            foreach (var proc in procedures)
            {
                ExecSp(proc);
            }

            m_log.Info("Procedures execution completed");
        }

        private void ExecSp(string proc)
        {
            try
            {
                m_log.Info($"Starting execution of procedure '{proc}'");

                var returned = m_database.Sql().Call(proc).WithParam("@projectId", m_session.Project.Id).Scalar();
                m_log.Info($"Execution of procedure {proc} returned {returned}");
            }
            catch (Exception ex)
            {
                m_log.Error($"Execution of procedure {proc} failed", ex);
            }

        }

        private IList<string> GetProcedures()
        {
            m_log.Info($"Searching stored procedures with prefix 'autosp_':");

            var procs = m_database.Sql().Execute("select name from sys.procedures where name like 'autosp[_]%'")
                .MapRows<string>(r => r.GetString(0));

            m_log.Info($"Found {procs.Count} of procedures with prefix 'autosp_'");

            return procs;
        }
    }
}
