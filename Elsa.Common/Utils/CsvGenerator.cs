using System;
using System.IO;
using System.Text;

namespace Elsa.Common.Utils
{
    public class CsvGenerator
    {
        private readonly StreamWriter m_writer;

        private readonly StringBuilder m_row = new StringBuilder();

        private readonly string[] m_columnIndex;

        private int m_cellIndex;

        public CsvGenerator(StreamWriter writer, string[] columnIndex, bool printHeader = true)
        {
            if (printHeader)
            {
                writer.WriteLine("\"verze 4\"");
                writer.WriteLine();
            }
            m_writer = writer;
            m_columnIndex = columnIndex;
        }

        public CsvGenerator CellOpt(params object[] values)
        {
            return NewCol(false, values);
        }

        public void WriteHeader()
        {
            foreach (var header in m_columnIndex)
            {
                CellMan(header);
            }
            CommitRow();
        }

        public CsvGenerator CellMan(params object[] values)
        {
            return NewCol(true, values);
        }

        private CsvGenerator NewCol(bool mandatory, params object[] values)
        {
            values = values ?? new object[0];

            object value = null;

            foreach (var val in values)
            {
                if (val == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(val.ToString()))
                {
                    value = val;
                    break;
                }
            }

            if (m_cellIndex > 0)
            {
                m_row.Append(",");
            }

            var strValue = string.Empty;
            if (value != null)
            {
                strValue = value.ToString();
                strValue = strValue.Replace("\"", "\"\"");
                strValue = string.Format("\"{0}\"", strValue);
            }

            if (mandatory && string.IsNullOrWhiteSpace(strValue))
            {
                throw new Exception("Neni hodnota pro sloupec " + m_columnIndex[m_cellIndex]);
            }

            m_row.Append(strValue);

            m_cellIndex++;

            return this;
        }

        public void CommitRow()
        {
            m_writer.Write(m_row.ToString());
            m_writer.Write("\n");
            RollbackRow();
        }

        public void RollbackRow()
        {
            m_row.Clear();
            m_cellIndex = 0;
        }
    }
}
