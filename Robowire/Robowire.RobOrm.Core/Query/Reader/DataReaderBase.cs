using System.Collections.Generic;
using System.Linq;

namespace Robowire.RobOrm.Core.Query.Reader
{
    public abstract class DataReaderBase : IDataReader
    {
        private List<string> m_columnIndex;
        
        protected DataReaderBase(string rootPath, List<string> columnIndex)
        {
            RootPath = rootPath;
            m_columnIndex = columnIndex;
        }

        protected DataReaderBase() : this(string.Empty, null) { }

        public string RootPath { get; protected set; }

        public IDataRecord GetDeeperReader(string pathSegment)
        {
            if (!string.IsNullOrWhiteSpace(RootPath))
            { 
                return CreateChildReader($"{RootPath}.{pathSegment}", m_columnIndex);
            }

            return CreateChildReader(pathSegment, m_columnIndex);
        }

        public T Get<T>(string columnName)
        {
            var index = GetColumnOrdinal(columnName);
            return index < 0 ? default(T) : GetValue<T>(index);
        }

        public T Get<T>(int columnOrdinal)
        {
            return GetValue<T>(columnOrdinal);
        }

        public bool IsNull(string columnName)
        {
            var index = GetColumnOrdinal(columnName);
            return (index < 0) || GetIsNull(index);
        }

        public bool Read()
        {
            var result = NextRecord();

            //if (result && m_columnIndex == null)
            //{
            //    CreateColumnIndex();
            //}

            return result;
        }

        private List<string> GetColumnIndex()
        {
            if (m_columnIndex == null)
            {
                m_columnIndex = GetColumnsOrder().ToList();
            }

            return m_columnIndex;
        }

        private int GetColumnOrdinal(string columnName)
        {
            if (!string.IsNullOrWhiteSpace(RootPath))
            {
                columnName = $"{RootPath}.{columnName}";
            }

            return GetColumnIndex().IndexOf(columnName);
        }

        protected abstract IDataReader CreateChildReader(string childPath, List<string> columnIndex);

        protected abstract bool GetIsNull(int column);

        protected abstract T GetValue<T>(int column);

        protected abstract bool NextRecord();

        protected abstract IEnumerable<string> GetColumnsOrder();

        public abstract void Dispose();
    }
}
