using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Dapper;
using Robowire.RobOrm.Core.NonOrm;

namespace Robowire.RobOrm.SqlServer.Pure
{
    public class SqlCommandBuilder : ISqlBuilder, ISqlExecutor
    {
        
        private readonly List<SqlParameter> m_parameters = new List<SqlParameter>();

        private readonly List<Action<SqlParameterCollection>> m_paramsCallbacks = new List<Action<SqlParameterCollection>>();
        private readonly List<Action<SqlCommand>> m_commandSetup = new List<Action<SqlCommand>>();
        private string m_commandText;
        private CommandType m_commandType;

        private readonly IExecutor m_executor;

        public SqlCommandBuilder(IExecutor executor)
        {
            m_executor = executor;
        }

        public ISqlExecutor Call(string storedProcedureName)
        {
            m_commandText = storedProcedureName;
            m_commandType = CommandType.StoredProcedure;

            return this;
        }

        public ISqlExecutor Execute(string sql)
        {
            m_commandText = sql;
            m_commandType = CommandType.Text;

            return this;
        }

        public ISqlExecutor ExecuteWithParams(string sql, params object[] parameter)
        {
            if (parameter == null)
            {
                return Execute(sql);
            }

            for (var i = 0; i < parameter.Length; i++)
            {
                var paramName = $"@p{i}";
                WithParam(paramName, parameter[i]);

                var placeHolder = $"{{{i}}}";

                if (!sql.Contains(placeHolder))
                {
                    throw new InvalidOperationException($"Placeholder \"{placeHolder}\" expected");
                }

                sql = sql.Replace(placeHolder, paramName);
            }

            return Execute(sql);
        }

        public ISqlExecutor WithParam(string paramName, object value)
        {
            if (value == null)
            {
                value = DBNull.Value;
            }

            m_parameters.Add(new SqlParameter(paramName, value));
            return this;
        }

        public ISqlExecutor WithParam(SqlParameter parameter)
        {
            m_parameters.Add(parameter);
            return this;
        }

        public ISqlExecutor WithParams(Action<SqlParameterCollection> paramsCollection)
        {
            m_paramsCallbacks.Add(paramsCollection);
            return this;
        }

        public ISqlExecutor WithOptionalParam(Func<bool> includeParameter, string name, object value)
        {
            return WithOptionalParam(includeParameter, name, () => value);
        }

        public ISqlExecutor WithOptionalParam(Func<bool> includeParameter, string name, Func<object> value)
        {
            return includeParameter() ? WithParam(name, value()) : this;
        }

        public ISqlExecutor WithStructuredParam(string parameterName, string dataTypeName, DataTable table)
        {
            var parameter = new SqlParameter
                                {
                                    ParameterName = parameterName,
                                    TypeName = dataTypeName,
                                    SqlDbType = SqlDbType.Structured,
                                    Value = table
                                };
            m_parameters.Add(parameter);

            return this;
        }

        public ISqlExecutor WithStructuredParam(string parameterName, DataTable table)
        {
            return WithStructuredParam(parameterName, table.TableName, table);
        }

        public ISqlExecutor WithStructuredParam<T>(
            string parameterName,
            string dataTypeName,
            IEnumerable<T> collection,
            IEnumerable<string> dataTypeAttributes,
            Func<T, object[]> rowGenerator)
        {
            var table = new DataTable(dataTypeName);

            foreach (var attribute in dataTypeAttributes)
            {
                table.Columns.Add(attribute);
            }

            foreach (var row in collection)
            {
                table.Rows.Add(rowGenerator(row));
            }

            return WithStructuredParam(parameterName, dataTypeName, table);
        }

        public ISqlExecutor SetupCommand(Action<SqlCommand> setupCommand)
        {
            m_commandSetup.Add(setupCommand);
            return this;
        }

        public void Read(Action<DbDataReader> readerAction)
        {
            Execute<object>(
                cmd =>
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            readerAction(reader);
                        }

                        return null;
                    });
        }
        
        public T Read<T>(Func<DbDataReader, T> readerAction)
        {
            return Execute<T>(
                cmd =>
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        return readerAction(reader);
                    }
                });
        }
        
        public void ReadRows(Action<DbDataReader> rowAction)
        {
            Read(
                reader =>
                    {
                        while (reader.Read())
                        {
                            rowAction(reader);
                        }
                    });
        }
        
        public IList<T> MapRows<T>(Func<DbDataReader, T> rowMapper)
        {
            var result = new List<T>();

            ReadRows(row => result.Add(rowMapper(row)));

            return result;
        }
        
        public object Scalar()
        {
            return Execute(c => c.ExecuteScalar());
        }

        public T Scalar<T>()
        {
            var res = Scalar();
            if ((res == null) || (DBNull.Value.Equals(res) && ((Nullable.GetUnderlyingType(typeof(T)) != null) || typeof(T).IsClass)))
            {
                return default(T);
            }

            return (T)res;
        }

        public int NonQuery()
        {
            return Execute(c => c.ExecuteNonQuery());
        }
        
        public DataTable Table()
        {
            return Execute(
                c =>
                    {
                        using (var adapter = new SqlDataAdapter(c))
                        {
                            var table = new DataTable();
                            adapter.Fill(table);
                            return table;
                        }
                    });
        }

        public DataSet DataSet()
        {
            return Execute(
                c =>
                {
                    using (var adapter = new SqlDataAdapter(c))
                    {
                        var set = new DataSet();
                        adapter.Fill(set);
                        return set;
                    }
                });
        }
        
        private void SetupCommand(SqlCommand command)
        {
            if (string.IsNullOrWhiteSpace(m_commandText))
            {
                throw new InvalidOperationException("No command specified. Use Execute, Call or ExecuteWithParams method.");
            }

            command.CommandType = m_commandType;
            command.CommandText = m_commandText;

            foreach (var cs in m_commandSetup)
            {
                cs.Invoke(command);
            }

            command.Parameters.AddRange(m_parameters.ToArray());

            foreach (var paramsCallback in m_paramsCallbacks)
            {
                paramsCallback(command.Parameters);
            }
        }

        private T Execute<T>(Func<SqlCommand, T> action)
        {
            return m_executor.Execute(SetupCommand, action);
        }

        #region ReadRows with callback
        public void ReadRows<T1>(Action<T1> rowCallback)
        {
            ReadRows(row => rowCallback(GetFieldValue<T1>(row, 0)));
        }

        public void ReadRows<T1, T2>(Action<T1, T2> rowCallback)
        {
            ReadRows(row => rowCallback(GetFieldValue<T1>(row, 0), GetFieldValue<T2>(row, 1)));
        }

        public void ReadRows<T1, T2, T3>(Action<T1, T2, T3> rowCallback)
        {
            ReadRows(
                row => rowCallback(GetFieldValue<T1>(row, 0), GetFieldValue<T2>(row, 1), GetFieldValue<T3>(row, 2)));
        }

        public void ReadRows<T1, T2, T3, T4>(Action<T1, T2, T3, T4> rowCallback)
        {
            ReadRows(
                row =>
                    rowCallback(
                        GetFieldValue<T1>(row, 0),
                        GetFieldValue<T2>(row, 1),
                        GetFieldValue<T3>(row, 2),
                        GetFieldValue<T4>(row, 3)));
        }

        public void ReadRows<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> rowCallback)
        {
            ReadRows(
                row =>
                    rowCallback(
                        GetFieldValue<T1>(row, 0),
                        GetFieldValue<T2>(row, 1),
                        GetFieldValue<T3>(row, 2),
                        GetFieldValue<T4>(row, 3),
                        GetFieldValue<T5>(row, 4)));
        }

        public void ReadRows<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> rowCallback)
        {
            ReadRows(
                row =>
                    rowCallback(
                        GetFieldValue<T1>(row, 0),
                        GetFieldValue<T2>(row, 1),
                        GetFieldValue<T3>(row, 2),
                        GetFieldValue<T4>(row, 3),
                        GetFieldValue<T5>(row, 4),
                        GetFieldValue<T6>(row, 5)));
        }

        public void ReadRows<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> rowCallback)
        {
            ReadRows(
                row =>
                    rowCallback(
                        GetFieldValue<T1>(row, 0),
                        GetFieldValue<T2>(row, 1),
                        GetFieldValue<T3>(row, 2),
                        GetFieldValue<T4>(row, 3),
                        GetFieldValue<T5>(row, 4),
                        GetFieldValue<T6>(row, 5),
                        GetFieldValue<T7>(row, 6)));
        }

        public void ReadRows<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> rowCallback)
        {
            ReadRows(
                row =>
                    rowCallback(
                        GetFieldValue<T1>(row, 0),
                        GetFieldValue<T2>(row, 1),
                        GetFieldValue<T3>(row, 2),
                        GetFieldValue<T4>(row, 3),
                        GetFieldValue<T5>(row, 4),
                        GetFieldValue<T6>(row, 5),
                        GetFieldValue<T7>(row, 6),
                        GetFieldValue<T8>(row, 7)));
        }

        public void ReadRows<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> rowCallback)
        {
            ReadRows(
                row =>
                    rowCallback(
                        GetFieldValue<T1>(row, 0),
                        GetFieldValue<T2>(row, 1),
                        GetFieldValue<T3>(row, 2),
                        GetFieldValue<T4>(row, 3),
                        GetFieldValue<T5>(row, 4),
                        GetFieldValue<T6>(row, 5),
                        GetFieldValue<T7>(row, 6),
                        GetFieldValue<T8>(row, 7),
                        GetFieldValue<T9>(row, 8)));
        }

        public void ReadRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> rowCallback)
        {
            ReadRows(
                row =>
                    rowCallback(
                        GetFieldValue<T1>(row, 0),
                        GetFieldValue<T2>(row, 1),
                        GetFieldValue<T3>(row, 2),
                        GetFieldValue<T4>(row, 3),
                        GetFieldValue<T5>(row, 4),
                        GetFieldValue<T6>(row, 5),
                        GetFieldValue<T7>(row, 6),
                        GetFieldValue<T8>(row, 7),
                        GetFieldValue<T9>(row, 8),
                        GetFieldValue<T10>(row, 9)));
        }

        #endregion

        private static T GetFieldValue<T>(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                if (typeof(T).IsValueType && (Nullable.GetUnderlyingType(typeof(T)) == null))
                {
                    throw new InvalidOperationException($"Cannot convert NULL value to  {typeof(T)}. Use nullable type");
                }

                return default(T);
            }

            return reader.GetFieldValue<T>(ordinal);
        }

        public List<T> AutoMap<T>()
        {
            return Read<List<T>>(reader => 
            {
                var result = new List<T>();

                var parser = reader.GetRowParser<T>(typeof(T));

                while (reader.Read())
                {
                    var parsedRow = parser(reader);
                    result.Add(parsedRow);
                }

                return result;
            });
        }
    }
}
