using System;
using System.Data.SqlClient;

using Robowire.RobOrm.SqlServer.Transaction;

namespace Robowire.RobOrm.SqlServer
{
    public class ConnectionCreator : SqlTransactionManagerBase
    {
        private readonly ISqlConnectionStringProvider m_connectionStringProvider;

        public ConnectionCreator(ISqlConnectionStringProvider connectionStringProvider)
        {
            m_connectionStringProvider = connectionStringProvider;
        }

        protected override Func<SqlConnection> ConnectionFactory
        {
            get
            {
                return () =>
                {
                    return new SqlConnection(m_connectionStringProvider.ConnectionString);
                };
            }
        }

        public override SqlConnection OpenUnmanagedConnection()
        {
            return ConnectionFactory();
        }
    }
}
