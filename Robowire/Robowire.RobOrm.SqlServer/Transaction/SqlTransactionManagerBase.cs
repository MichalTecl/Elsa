using System;
using System.Data.SqlClient;
using System.Threading;

using Robowire.RobOrm.Core;

namespace Robowire.RobOrm.SqlServer.Transaction
{
    public abstract class SqlTransactionManagerBase : ITransactionManager<SqlConnection>
    {
        private readonly ThreadLocal<ISqlTransaction> m_threadTransaction= new ThreadLocal<ISqlTransaction>();

        protected abstract Func<SqlConnection> ConnectionFactory { get; }

        public ITransaction<SqlConnection> Open(bool childOnly)
        {
            var parent = m_threadTransaction.Value;

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
            
            m_threadTransaction.Value = child;

            return child;
        }

        public abstract SqlConnection OpenUnmanagedConnection();

        public void RemoveCurrentTransaction()
        {
            var child = m_threadTransaction.Value;
            if (child == null)
            {
                return;
            }
            
            m_threadTransaction.Value = child.Parent;
        }
    }
}
