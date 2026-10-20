using System;
using System.Collections.Generic;

namespace Elsa.App.OrdersInfo.Model
{
    public class AggregatedResultModel
    {
        private readonly List<string> _rows = new List<string>();
        private readonly List<string> _columns = new List<string>();

        // <RowTitle, <ColumnTitle, Value>>
        public Dictionary<string, Dictionary<string, string>> Table { get; } = new Dictionary<string, Dictionary<string, string>>();

        // Contains a legend of presented data
        public string Legend { get; set; }

        // Title of the first column, which contains row titles
        public string RowsTitle { get; set; }

        // Row titles in display order
        public IReadOnlyList<string> Rows => _rows;

        // Column titles in display order
        public IReadOnlyList<string> Columns => _columns;

        public void DefineRows(IEnumerable<string> rows)
        {
            // defines all rows in the aggregation. This call defines order of rows in the report. Duplicities throw exception
            AddUnique(_rows, rows, "Řádek");

            foreach (var row in _rows)
            {
                if (!Table.ContainsKey(row))
                    Table[row] = new Dictionary<string, string>();
            }
        }

        public void DefineColumns(IEnumerable<string> columns)
        {
            // defines all columns in the aggregation, their display order. Duplicities throw exception
            AddUnique(_columns, columns, "Sloupec");
        }

        public void Write(string row, string column, string value, bool tolerateOverwrite = false)
        {
            // writes a value to the specified position, if the value was already set and tolerateOverwrite = false, throw
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (!Table.TryGetValue(row, out var rowValues))
                throw new ArgumentException($"Řádek '{row}' není v agregaci definován");

            if (!_columns.Contains(column))
                throw new ArgumentException($"Sloupec '{column}' není v agregaci definován");

            if (!tolerateOverwrite && rowValues.ContainsKey(column))
                throw new InvalidOperationException($"Hodnota pro řádek '{row}' a sloupec '{column}' již byla zapsána");

            rowValues[column] = value;
        }

        public string Read(string row, string column)
        {
            if (row == null || column == null)
                return null;

            return Table.TryGetValue(row, out var rowValues) && rowValues.TryGetValue(column, out var value)
                ? value
                : null;
        }

        private static void AddUnique(List<string> target, IEnumerable<string> values, string kind)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            foreach (var value in values)
            {
                if (value == null)
                    throw new ArgumentException($"{kind} agregace nesmí mít prázdný název");

                if (target.Contains(value))
                    throw new ArgumentException($"{kind} '{value}' je v agregaci definován vícekrát");

                target.Add(value);
            }
        }
    }
}
