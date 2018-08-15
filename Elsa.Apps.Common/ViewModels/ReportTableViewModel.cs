using System.Collections.Generic;
using System.Linq;

namespace Elsa.Apps.Common.ViewModels
{
    public class ReportTableViewModel
    {
        public string ReportName { get; set; }

        public string ReportDescription { get; set; }

        public List<ReportColumnDefinition> Columns { get; } = new List<ReportColumnDefinition>();

        public List<ReportRow> Rows { get; } = new List<ReportRow>();

        public string this[string rowId, string columnId]
        {
            get
            {
                var row = GetRow(rowId);
                var columnIndex = GetColumnIndex(columnId);

                return row.Values[columnIndex];
            }

            set
            {
                var row = GetRow(rowId);
                var columnIndex = GetColumnIndex(columnId);

                row.Values[columnIndex] = value;
            }
        }

        private ReportRow GetRow(string id)
        {
            var row = Rows.FirstOrDefault(r => r.RowId == id);
            if (row == null)
            {
                row = new ReportRow() { RowId = id };
                Rows.Add(row);
                EnsureValues(row);
            }

            return row;
        }

        private void EnsureValues(ReportRow row)
        {
            while (row.Values.Count < Columns.Count)
            {
                row.Values.Add(string.Empty);
            }
        }

        private int GetColumnIndex(string columnId)
        {
            var i = 0;

            for (; i < Columns.Count; i++)
            {
                if (Columns[i].ColumnId == columnId)
                {
                    return i;
                }
            }

            Columns.Add(new ReportColumnDefinition()
                            {
                                ColumnId = columnId
                            });

            foreach (var row in Rows)
            {
                EnsureValues(row);
            }

            return i;
        }
    }
}
