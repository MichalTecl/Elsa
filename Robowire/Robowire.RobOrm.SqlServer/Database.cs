using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.Core.EntityModel;
using Robowire.RobOrm.Core.Internal;
using Robowire.RobOrm.Core.NonOrm;
using Robowire.RobOrm.Core.Query.Abstraction;
using Robowire.RobOrm.Core.Query.Model;

namespace Robowire.RobOrm.SqlServer
{
    public class Database : DatabaseBase<SqlConnection>
    {
        private readonly ITransactionManager<SqlConnection> m_connectionFactory;

        public Database(IServiceLocator locator, IDataModelHelper dataModel, ITransactionManager<SqlConnection> connectionFactory)
            : base(locator, dataModel, connectionFactory)
        {
            m_connectionFactory = connectionFactory;
        }

        public override string GetQueryText<T>(IQueryModel<T> model, IQueryBuilder<T> builder) 
        {
            return SqlQueryRenderer.Render(model, builder);
        }

        public override ISqlBuilder Sql()
        {
            return new Pure.SqlCommandBuilder(this);
        }

        protected override IDataReader ExecuteReader<T>(IQueryModel<T> model, IQueryBuilder<T> builder, ITransaction<SqlConnection> transaction) 
        {
            var commandText = SqlQueryRenderer.Render(model, builder);

            var connection = transaction.GetConnection();
            var command = new SqlCommand(commandText, connection);
            var parameters = command.Parameters;

            foreach (var p in builder.GetParameters())
            {
                parameters.AddWithValue(p.Key, p.Value);
            }

            var sqlReader = command.ExecuteReader();
            return new HierarchicSqlDataReader(sqlReader, null);
        }

        protected override object InsertEntity(IEntity entity, ITransaction<SqlConnection> transaction)
        {
            var values = entity.GetValues().Where(v => !v.IsPk).ToList();

            var columnsList = string.Join(", ", values.Select(i => $"[{i.ColumnName}]"));
            var paramList = string.Join(", ", values.Select(i => $"@{i.ColumnName}"));

            var sql =
                $"INSERT INTO [{entity.DbEntityName}] ({columnsList}) VALUES ({paramList}); SELECT CAST(SCOPE_IDENTITY() AS {SqlTypeMapper.GetSqlTypeName(entity.PrimaryKeyType, 0)});";

            using (var command = new SqlCommand(sql, transaction.GetConnection()))
            {
                foreach (var entityColumnValue in values)
                {
                    command.Parameters.AddWithValue($"@{entityColumnValue.ColumnName}", entityColumnValue.Value ?? DBNull.Value);
                }

                var newPk = command.ExecuteScalar();
                entity.PrimaryKeyValue = newPk;

                return newPk;
            }
        }

        protected override void UpdateEntity(IEntity entity, ITransaction<SqlConnection> transaction)
        {
            var values = entity.GetValues().Where(v => !v.IsPk).ToList();

            var sb = new StringBuilder("UPDATE [");
            sb.Append(entity.DbEntityName).Append("] SET ");

            using (var command = new SqlCommand())
            {
                for (var v = 0; v < values.Count; v++)
                {
                    if (v > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append("[").Append(values[v].ColumnName).Append("] = @p").Append(v);


                    command.Parameters.AddWithValue($"@p{v}", values[v].Value ?? DBNull.Value);
                }

                var pk = entity.GetValues().Single(pkv => pkv.IsPk);

                sb.Append(" WHERE [").Append(pk.ColumnName).Append("] = @pk");

                command.Parameters.AddWithValue("@pk", pk.Value);

                command.Connection = transaction.GetConnection();
                command.CommandText = sb.ToString();

                command.ExecuteNonQuery();
            }
        }

        protected override object UpsertEntity(IEntity entity, ITransaction<SqlConnection> transaction)
        {
            throw new NotImplementedException();
        }

        protected override void DeleteEntity(IEntity entity, ITransaction<SqlConnection> transaction)
        {
            var pk = entity.GetValues().Single(i => i.IsPk);

            var sql = $"DELETE FROM [{entity.DbEntityName}] WHERE [{pk.ColumnName}] = @pk";

            using (var cmd = new SqlCommand(sql, transaction.GetConnection()))
            {
                cmd.Parameters.AddWithValue("@pk", pk.Value);
                cmd.ExecuteNonQuery();
            }
        }

        protected override object ExecuteScalar(string query, Action<DbParameterCollection> setParameters, ITransaction<SqlConnection> transaction)
        {
            using (var cmd = new SqlCommand(query, transaction.GetConnection()))
            {
                setParameters(cmd.Parameters);

                return cmd.ExecuteScalar();
            }
        }

        protected override T Execute<T>(
            Action<SqlCommand> setupCommand,
            Func<SqlCommand, T> action,
            ITransaction<SqlConnection> transaction)
        {
            using (var cmd = new SqlCommand())
            {
                cmd.Connection = transaction.GetConnection();
                setupCommand(cmd);

                return action(cmd);
            }
        }
    }
}
