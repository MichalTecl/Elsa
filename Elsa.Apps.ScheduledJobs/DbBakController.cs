using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;

namespace Elsa.Apps.ScheduledJobs
{
    [Controller("dbbak")]
    public class DbBakController : ElsaControllerBase
    {
        private readonly ITransactionManager<SqlConnection> m_database;

        public DbBakController(IWebSession webSession, ILog log, ITransactionManager<SqlConnection> database) : base(webSession, log)
        {
            m_database = database;
        }

        public FileResult Get()
        {
            string fileName;

            using (var conn = m_database.OpenUnmanagedConnection())
            {
                conn.Open();

                using (var cmd = new SqlCommand("sp_backup", conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    fileName = cmd.ExecuteScalar() as string;

                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        throw new InvalidOperationException("Backup failed");
                    }
                }
            }

            return new FileResult(Path.GetFileName(fileName), File.ReadAllBytes(fileName));
        }
    }
}
