using System.Collections.Generic;
using System.Data.SqlClient;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.Core.Query.Reader;

namespace Robowire.RobOrm.SqlServer
{
    public class HierarchicSqlDataReader : DataReaderBase
    {
        private List<string> m_columnIndex;
        private readonly SqlDataReader m_reader;

        public HierarchicSqlDataReader(SqlDataReader reader, string path) : this(reader, path, null)
        {
        }

        private HierarchicSqlDataReader(SqlDataReader reader, string path, List<string> columnIndex)
        {
            m_reader = reader;
            RootPath = path;
            m_columnIndex = columnIndex;
        }

        public override void Dispose()
        {
            m_reader.Dispose();
        }

        protected override IDataReader CreateChildReader(string childPath, List<string> columnIndex)
        {
            return new HierarchicSqlDataReader(m_reader, childPath, columnIndex);
        }

        protected override bool GetIsNull(int column)
        {
            return m_reader.IsDBNull(column);
        }

        protected override T GetValue<T>(int column)
        {
            return m_reader.GetFieldValue<T>(column);
        }

        protected override bool NextRecord()
        {
            return m_reader.Read();
        }

        protected override IEnumerable<string> GetColumnsOrder()
        {
            if (m_columnIndex == null)
            {
                m_columnIndex = new List<string>(m_reader.FieldCount);
                for (var i = 0; i < m_reader.FieldCount; i++)
                {
                    m_columnIndex.Add(m_reader.GetName(i));
                }
            }

            return m_columnIndex;
        }
    }
}
