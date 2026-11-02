using System.Data.SqlClient;

namespace RobOrm.SqlServer.Transaction
{
    internal sealed class ChildSqlTransaction : ISqlTransaction
    {
        private bool _commited;
        private readonly SqlTransactionManagerBase _owner;

        internal ChildSqlTransaction(ISqlTransaction parent, SqlTransactionManagerBase owner)
        {
            Parent = parent;
            _owner = owner;
        }

        public SqlConnection GetConnection()
        {
            return Parent.GetConnection();
        }

        public void Commit()
        {
            _commited = true;
        }

        public ISqlTransaction Parent { get; }

        public void Rollback()
        {
            Parent.Rollback();
        }

        public void Dispose()
        {
            if (!_commited)
            {
                Parent.Rollback();
            }

            _owner.RemoveCurrentTransaction();
        }
    }
}
