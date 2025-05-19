using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Razor.Parser;

namespace Elsa.Common.DbUtils
{
    public class ScriptGenerator
    {
        private static readonly Dictionary<Type, string> _sqlTypes = new Dictionary<Type, string>
        {
            { typeof(int), "INT" },
            { typeof(long), "BIGINT" },
            { typeof(short), "SMALLINT" },
            { typeof(byte), "TINYINT" },
            { typeof(bool), "BIT" },
            { typeof(decimal), "DECIMAL(18,2)" },
            { typeof(float), "REAL" },
            { typeof(double), "FLOAT" },
            { typeof(string), "NVARCHAR(1000)" },
            { typeof(DateTime), "DATETIME" },
            { typeof(Guid), "UNIQUEIDENTIFIER" }
        };

        private readonly Dictionary<string, List<Dictionary<string, object>>> _tables = new Dictionary<string, List<Dictionary<string, object>>>();
        private Dictionary<string, string> _colTypes = new Dictionary<string, string>();
        

        public void WriteCell(string tableName, string columnName, object value)
        {
            if(!_tables.TryGetValue(tableName, out var table))
            {
                table = new List<Dictionary<string, object>>();
                _tables.Add(tableName, table);
                NewRow(tableName);
            }

            var row = table.Last();

            row[columnName] = value;

            var colKey = $"{tableName}.{columnName}";
            if (value != null && !_colTypes.ContainsKey(colKey))
            {
                var valueType = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();

                if (!_sqlTypes.TryGetValue(valueType, out var sqlType))
                    throw new ArgumentException($"Cannot map sql type {valueType}");

                _colTypes[colKey] = sqlType;
            }

            if (value == null)
            {
                row[columnName] = "NULL";
            } 
            else if (value is string strVal)
            {
                row[columnName] = $"N'{strVal}'";
            }
            else if (value is DateTime dtVal)
            {
                row[columnName] = $"'{dtVal:yyyy-MM-dd HH:mm}'";
            }
            else
            {
                row[columnName] = value.ToString();
            }
        }

        public void WriteRow(string tableName, object rowModel)
        {
            foreach(var prop in rowModel.GetType().GetProperties())
                WriteCell(tableName, prop.Name, prop.GetValue(rowModel, null));

            NewRow(tableName);
        }

        public void NewRow(string tableName)
        {
            if (!_tables.TryGetValue(tableName, out var table))
                return;

            table.Add(new Dictionary<string, object>());
        }

        public string GetScript()
        {
            var sb = new StringBuilder();

            var tables = _tables.OrderBy(c => c.Value.Count).ToList();

            foreach (var table in tables)
            {
                sb.AppendLine($"/*** {table.Key} *****/");
                sb.Append($"DECLARE @{table.Key} TABLE(");

                var columnNames = _colTypes.Where(c => c.Key.StartsWith($"{table.Key}.")).Select(tc => tc.Key.Split('.').Last()).ToList();

                sb.Append(string.Join(", ", columnNames.Select(cn => $"{cn} {_colTypes[table.Key + "." + cn]}")));
                sb.AppendLine(");");

                sb.AppendLine();
                sb.Append("INSERT INTO @").Append(table.Key).Append(" (").Append(string.Join(", ", columnNames)).AppendLine(")").AppendLine("VALUES");

                var firstRow = true;

                foreach (var row in table.Value)
                {
                    if (row.Count == 0)
                        continue;

                    if (!firstRow)
                        sb.AppendLine(",");
                    firstRow = false;

                    sb.Append("   (");

                    bool firstCol = true;
                    foreach (var column in columnNames)
                    {
                        var value = row[column];
                        if (!firstCol)
                            sb.Append(", ");
                        firstCol = false;

                        sb.Append(value);
                    }

                    sb.Append(")");
                }

                sb.AppendLine(";").AppendLine();
            }     
            
            return sb.ToString();
        }
    }
}
