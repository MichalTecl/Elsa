using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure
{
    public static class ColumnHeadControlLoader
    {
        internal static void Render(string columnId, string title, string cellClass, bool canSort, Func<string, string> mapPath, StringBuilder sb)
        {
            var sorter = string.Empty;
            if (canSort)
            {
                sorter = File.ReadAllText(mapPath("/UI/DistributorsApp/Parts/DistributorGridParts/ColumnHeadSorterTemplate.html"));

                sorter = sorter.Replace("{columnId}", columnId);
            }

            sb.AppendLine($"<div class=\"{cellClass}\"><div class=\"gridHeadTitle\">{title}</div>{sorter}</div>");
        }
    }
}
