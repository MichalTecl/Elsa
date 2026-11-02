using System;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;

namespace RobOrm.SqlServer.Transaction
{
    internal sealed class SqlTransaction : ISqlTransaction
    {
        private bool _rolledBack = false;

        private readonly Func<SqlConnection> _connectionFactory;

        private SqlConnection _connection;
        private readonly TransactionScope _scope;
        private readonly SqlTransactionManagerBase _owner;

        public SqlTransaction(Func<SqlConnection> connectionFactory, SqlTransactionManagerBase owner, bool withoutScope)
        {
            _connectionFactory = connectionFactory;
            _owner = owner;

            _scope = withoutScope ? null : CreateTransactionScope();
        }

        public void Dispose()
        {
            try
            {
                _connection?.Dispose();
            }
            catch{;}
            finally
            {
                try
                {
                    _scope?.Dispose();
                }
                catch{;}
                finally
                {
                    _owner.RemoveCurrentTransaction();
                }
            }
        }

        public SqlConnection GetConnection()
        {
            _connection = _connection ?? _connectionFactory();
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }

            return _connection;
        }

        public void Commit()
        {
            if (_rolledBack)
            {

                throw new InvalidOperationException("Cannot commit the transaction because some child transaction is uncommited");
            }

            _scope?.Complete();
        }

        public ISqlTransaction Parent => null;

        public void Rollback()
        {
            _rolledBack = true;
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
