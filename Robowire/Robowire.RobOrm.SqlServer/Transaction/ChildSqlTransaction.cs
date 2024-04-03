using System.Data.SqlClient;

namespace Robowire.RobOrm.SqlServer.Transaction
{
    internal sealed class ChildSqlTransaction : ISqlTransaction
    {
        private bool m_commited;
        private readonly SqlTransactionManagerBase m_owner;

        internal ChildSqlTransaction(ISqlTransaction parent, SqlTransactionManagerBase owner)
        {
            Parent = parent;
            m_owner = owner;
        }

        public SqlConnection GetConnection()
        {
            return Parent.GetConnection();
        }

        public void Commit()
        {
            m_commited = true;
        }

        public ISqlTransaction Parent { get; }

        public void Rollback()
        {
            Parent.Rollback();
        }

        public void Dispose()
        {
            if (!m_commited)
            {
                Parent.Rollback();
            }

            m_owner.RemoveCurrentTransaction();
        }
    }
}
