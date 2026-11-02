using System;
using System.Data.SqlClient;
using System.Threading;

using RobOrm.Core;

namespace RobOrm.SqlServer.Transaction
{
    public abstract class SqlTransactionManagerBase : ITransactionManager<SqlConnection>
    {
        private readonly ThreadLocal<ISqlTransaction> _threadTransaction= new ThreadLocal<ISqlTransaction>();

        protected abstract Func<SqlConnection> ConnectionFactory { get; }

        public ITransaction<SqlConnection> Open(bool childOnly)
        {
            var parent = _threadTransaction.Value;

            ISqlTransaction child = null; 
            //parent == null ? (ISqlTransaction)new SqlTransaction(ConnectionFactory, this) : new ChildSqlTransaction(parent, this);

            if (parent != null)
            {
                child = new ChildSqlTransaction(parent, this);
            }
            else
            {
                child = new SqlTransaction(ConnectionFactory, this, childOnly);
            }
            
            _threadTransaction.Value = child;

            return child;
        }

        public abstract SqlConnection OpenUnmanagedConnection();

        public void RemoveCurrentTransaction()
        {
            var child = _threadTransaction.Value;
            if (child == null)
            {
                return;
            }
            
            _threadTransaction.Value = child.Parent;
        }
    }
}
