using System;
using System.Collections.Generic;
using System.Data.SqlClient;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.Core.Query.Reader;

namespace Robowire.RobOrm.SqlServer
{
    public class HierarchicSqlDataReader : DataReaderBase
    {
        private List<string> _columnIndex;
        private readonly SqlDataReader _reader;
        private readonly Action<SqlException> _onError;
        private readonly SqlCommand _ownedCommand;

        public HierarchicSqlDataReader(SqlDataReader reader, string path) : this(reader, path, null, null, null)
        {
        }

        internal HierarchicSqlDataReader(SqlDataReader reader, string path, Action<SqlException> onError, SqlCommand ownedCommand)
            : this(reader, path, null, onError, ownedCommand)
        {
        }

        private HierarchicSqlDataReader(SqlDataReader reader, string path, List<string> columnIndex, Action<SqlException> onError, SqlCommand ownedCommand)
        {
            _reader = reader;
            _onError = onError;
            _ownedCommand = ownedCommand;
            RootPath = path;
            _columnIndex = columnIndex;
        }

        public override void Dispose()
        {
            try { _reader.Dispose(); }
            catch (SqlException ex) { _onError?.Invoke(ex); throw; }
            finally { _ownedCommand?.Dispose(); }
        }

        protected override IDataReader CreateChildReader(string childPath, List<string> columnIndex)
        {
            return new HierarchicSqlDataReader(_reader, childPath, columnIndex, _onError, null);
        }

        protected override bool GetIsNull(int column)
        {
            try { return _reader.IsDBNull(column); }
            catch (SqlException ex) { _onError?.Invoke(ex); throw; }
        }

        protected override T GetValue<T>(int column)
        {
            try { return _reader.GetFieldValue<T>(column); }
            catch (SqlException ex) { _onError?.Invoke(ex); throw; }
        }

        protected override bool NextRecord()
        {
            try { return _reader.Read(); }
            catch (SqlException ex) { _onError?.Invoke(ex); throw; }
        }

        protected override IEnumerable<string> GetColumnsOrder()
        {
            if (_columnIndex == null)
            {
                _columnIndex = new List<string>(_reader.FieldCount);
                for (var i = 0; i < _reader.FieldCount; i++)
                {
                    _columnIndex.Add(_reader.GetName(i));
                }
            }

            return _columnIndex;
        }
    }
}
