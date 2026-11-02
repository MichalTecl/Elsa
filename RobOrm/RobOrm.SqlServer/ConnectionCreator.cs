using System;
using System.Data.SqlClient;

using RobOrm.SqlServer.Transaction;

namespace RobOrm.SqlServer
{
    public class ConnectionCreator : SqlTransactionManagerBase
    {
        private readonly ISqlConnectionStringProvider _connectionStringProvider;

        public ConnectionCreator(ISqlConnectionStringProvider connectionStringProvider)
        {
            _connectionStringProvider = connectionStringProvider;
        }

        protected override Func<SqlConnection> ConnectionFactory
        {
            get
            {
                return () =>
                {
                    return new SqlConnection(_connectionStringProvider.ConnectionString);
                };
            }
        }

        public override SqlConnection OpenUnmanagedConnection()
        {
            return ConnectionFactory();
        }
    }
}
