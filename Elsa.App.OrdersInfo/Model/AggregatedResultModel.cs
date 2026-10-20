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

        // Message for the user displayed instead of the table (e.g. when the result cannot be produced for given filters)
        public string Message { get; set; }

        // <RowTitle, <ColumnTitle, Url>> - cells displayed as a link to any URL
        public Dictionary<string, Dictionary<string, string>> Links { get; } = new Dictionary<string, Dictionary<string, string>>();

        // <RowTitle, <ColumnTitle, DrilldownKey>> - cells displayed as a link to the list of orders behind the cell,
        // the key is passed back to IOrdersDrilldownAggregator.GetDrilldownOrderIds
        public Dictionary<string, Dictionary<string, string>> Drilldowns { get; } = new Dictionary<string, Dictionary<string, string>>();

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

        public void SetLink(string row, string column, string url)
        {
            SetCellValue(Links, row, column, url);
        }

        public void SetOrdersDrilldown(string row, string column, string drilldownKey)
        {
            SetCellValue(Drilldowns, row, column, drilldownKey);
        }

        public string Read(string row, string column)
        {
            if (row == null || column == null)
                return null;

            return Table.TryGetValue(row, out var rowValues) && rowValues.TryGetValue(column, out var value)
                ? value
                : null;
        }

        private void SetCellValue(Dictionary<string, Dictionary<string, string>> target, string row, string column, string value)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (!Table.ContainsKey(row))
                throw new ArgumentException($"Řádek '{row}' není v agregaci definován");

            if (!_columns.Contains(column))
                throw new ArgumentException($"Sloupec '{column}' není v agregaci definován");

            if (!target.TryGetValue(row, out var rowValues))
                target[row] = rowValues = new Dictionary<string, string>();

            rowValues[column] = value;
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
