using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Robowire.RobOrm.Core.NonOrm
{
    public interface ISqlExecutor
    {
        /// <summary>
        /// Adds a new parameter of specified name and value
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        ISqlExecutor WithParam(string paramName, object value);

        /// <summary>
        /// Adds provied parameter to the command
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        ISqlExecutor WithParam(SqlParameter parameter);

        /// <summary>
        /// Provides access to the Parameters collection of created SqlCommand object
        /// </summary>
        /// <returns></returns>
        ISqlExecutor WithParams(Action<SqlParameterCollection> paramsCollectionUpdater);

        /// <summary>
        /// Adds a new parameter of specified name and value if includeParameter is evaluated as true
        /// </summary>
        /// <param name="includeParameter"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        ISqlExecutor WithOptionalParam(Func<bool> includeParameter, string name, object value);

        /// <summary>
        /// Adds a new parameter of specified name and value if includeParameter is evaluated as true
        /// </summary>
        /// <param name="includeParameter"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        ISqlExecutor WithOptionalParam(Func<bool> includeParameter, string name, Func<object> value);


        ISqlExecutor WithStructuredParam(string parameterName, string dataTypeName, DataTable table);

        /// <summary>
        /// Note that DataTable.Name must be name of the User-defined type. Preferred usage is with UserDefinedTypeDescriptor, see <see cref="CommonUserDefinedTypes"></see>
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        ISqlExecutor WithStructuredParam(string parameterName, DataTable table);

        ISqlExecutor WithStructuredParam<T>(
            string parameterName,
            string dataTypeName,
            IEnumerable<T> collection,
            IEnumerable<string> dataTypeAttributes,
            Func<T, object[]> rowGenerator);

        ISqlExecutor SetupCommand(Action<SqlCommand> setupCommand);
        
        /// <summary>
        /// Creates a DataReader and executes readerAction with reference to it. Iteration over result rows should be done in readerAction
        /// </summary>
        /// <param name="readerAction"></param>
        void Read(Action<DbDataReader> readerAction);
        
        /// <summary>
        /// Creates a DataReader and invokes readerAction with reference to it. Return value of readerAction is returned as a result of Read invocation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="readerAction"></param>
        /// <returns></returns>
        T Read<T>(Func<DbDataReader, T> readerAction);

        /// <summary>
        /// Creates a DataReader and executes the rowAction for each result row
        /// </summary>
        /// <param name="rowAction"></param>
        void ReadRows(Action<DbDataReader> rowAction);

        /// <summary>
        /// Creates a dataReader, executes rowMapper for each result row and collects created objects to result collection 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rowMapper"></param>
        /// <returns></returns>
        IList<T> MapRows<T>(Func<DbDataReader, T> rowMapper);
        
        /// <summary>
        /// Executes the command and returns single value
        /// </summary>
        /// <returns></returns>
        object Scalar();

        T Scalar<T>();

        /// <summary>
        /// Executes the command and returns number of affected rows
        /// </summary>
        /// <returns></returns>
        int NonQuery();
        
        /// <summary>
        /// Executes the command and returns DataTable populated by result of the query
        /// </summary>
        /// <returns></returns>
        DataTable Table();
        
        /// <summary>
        /// Executes the command and returns DataSet populated by result of the query
        /// </summary>
        /// <returns></returns>
        DataSet DataSet();
        
        void ReadRows<T1>(Action<T1> rowCallback);
        void ReadRows<T1, T2>(Action<T1, T2> rowCallback);
        void ReadRows<T1, T2, T3>(Action<T1, T2, T3> rowCallback);
        void ReadRows<T1, T2, T3, T4>(Action<T1, T2, T3, T4> rowCallback);
        void ReadRows<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> rowCallback);
        void ReadRows<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> rowCallback);
        void ReadRows<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> rowCallback);
        void ReadRows<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> rowCallback);
        void ReadRows<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> rowCallback);
        void ReadRows<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> rowCallback);
    }
}
