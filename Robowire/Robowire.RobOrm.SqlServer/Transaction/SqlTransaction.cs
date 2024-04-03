using System;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;

namespace Robowire.RobOrm.SqlServer.Transaction
{
    internal sealed class SqlTransaction : ISqlTransaction
    {
        private bool m_rolledBack = false;

        private readonly Func<SqlConnection> m_connectionFactory;

        private SqlConnection m_connection;
        private readonly TransactionScope m_scope;
        private readonly SqlTransactionManagerBase m_owner;

        public SqlTransaction(Func<SqlConnection> connectionFactory, SqlTransactionManagerBase owner, bool withoutScope)
        {
            m_connectionFactory = connectionFactory;
            m_owner = owner;

            m_scope = withoutScope ? null : CreateTransactionScope();
        }

        public void Dispose()
        {
            try
            {
                m_connection?.Dispose();
            }
            catch{;}
            finally
            {
                try
                {
                    m_scope?.Dispose();
                }
                catch{;}
                finally
                {
                    m_owner.RemoveCurrentTransaction();
                }
            }
        }

        public SqlConnection GetConnection()
        {
            m_connection = m_connection ?? m_connectionFactory();
            if (m_connection.State == ConnectionState.Closed)
            {
                m_connection.Open();
            }

            return m_connection;
        }

        public void Commit()
        {
            if (m_rolledBack)
            {

                throw new InvalidOperationException("Cannot commit the transaction because some child transaction is uncommited");
            }

            m_scope?.Complete();
        }

        public ISqlTransaction Parent => null;

        public void Rollback()
        {
            m_rolledBack = true;
        }

        private TransactionScope CreateTransactionScope()
        {
            var options = new TransactionOptions
            {
                IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted
            };

            return new TransactionScope(asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled,
                transactionOptions: options,
                scopeOption: TransactionScopeOption.Required);
        }
    }
}
