using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Reporting
{
    internal class DynamicColumnNamesProcessor
    {
        private static string Month(List<string> arguments)
        {
            var offset = PollInt(arguments, "offset", 0);

            var theMonth = DateTime.Now.AddMonths(offset);

            return theMonth.ToString("MM/yyyy");
        }

        private static string Year(List<string> arguments)
        {
            var offset = PollInt(arguments, "offset", 0);

            var theMonth = DateTime.Now.AddYears(offset);

            return theMonth.ToString("yyyy");
        }

        private const string AutoColumnPrefix = "auto:";

        public static void SetDynamicColumnNames(DataTable table)
        {
            for (var c = 0; c < table.Columns.Count; c++)
            {
                var originalName = table.Columns[c].ColumnName;
                if (!originalName.StartsWith(AutoColumnPrefix))
                    continue;

                var theExpression = originalName.Length > AutoColumnPrefix.Length ? originalName.Substring(AutoColumnPrefix.Length).Trim() : string.Empty;

                theExpression = theExpression.Replace("()", string.Empty);

                var parts = theExpression.Split('(', ',', ')');

                var methodName = parts.FirstOrDefault()?.Trim() ?? string.Empty;

                var method = typeof(DynamicColumnNamesProcessor).GetMethod(methodName,
                    BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic);

                if (method == null)
                    throw new ArgumentException(
                        $"Cannot evaluate column expression \"{originalName}\" - method \"{methodName}\" was not found");

                var argulist = parts.Select(p => p.Trim()).Skip(1).ToList();

                var newName = method.Invoke(null, new object[] { argulist }) as string;

                if (string.IsNullOrWhiteSpace(newName))
                    throw new ArgumentException(
                        $"Evaluation of expression \"{originalName}\" resulted in \"{newName}\" - empty column name will not be used");

                while (table.Columns.Contains(newName))
                    newName = $"{newName}_";

                table.Columns[c].ColumnName = newName;
            }
        }
        
        private static int PollInt(List<string> arguments, string paramName, int? defaultValue = null)
        {
            if (!arguments.Any())
            {
                return defaultValue ?? throw new ArgumentException($"Required param '{paramName}' was not supplied");
            }

            var arg = arguments.First();
            arguments.RemoveAt(0);

            if (!int.TryParse(arg, out var parsed))
                throw new ArgumentException(
                    $"Cannot convert passed value \"{arg}\" to Int32 required by parameter '{paramName}'");

            return parsed;
        }
    }

}
