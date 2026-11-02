using System;
using System.Collections.Generic;
using System.Text;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using RobOrm.Core;

namespace Elsa.Jobs.AutomaticQueries
{
    public class AutoProceduresJob
    {
        private readonly ILog _log;
        private readonly IDatabase _database;
        private readonly ISession _session;

        public AutoProceduresJob(ILog log, IDatabase database, ISession session)
        {
            _log = log;
            _database = database;
            _session = session;
        }

        public void Run()
        {
            _log.Info($"Starting AutoProceduresJob");

            var procedures = GetProcedures();

            foreach (var proc in procedures)
            {
                ExecSp(proc);
            }

            _log.Info("Procedures execution completed");
        }

        private void ExecSp(string proc)
        {
            try
            {
                _log.Info($"Starting execution of procedure '{proc}'");

                var returned = _database.Sql().Call(proc).WithParam("@projectId", _session.Project.Id).Scalar();
                _log.Info($"Execution of procedure {proc} returned {returned}");
            }
            catch (Exception ex)
            {
                _log.Error($"Execution of procedure {proc} failed", ex);
            }

        }

        private IList<string> GetProcedures()
        {
            _log.Info($"Searching stored procedures with prefix 'autosp_':");

            var procs = _database.Sql().Execute("select name from sys.procedures where name like 'autosp[_]%'")
                .MapRows<string>(r => r.GetString(0));

            _log.Info($"Found {procs.Count} of procedures with prefix 'autosp_'");

            return procs;
        }
    }
}
