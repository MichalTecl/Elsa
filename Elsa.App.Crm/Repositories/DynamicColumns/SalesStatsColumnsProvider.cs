using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure;
using Elsa.Common.Caching;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class SalesStatsColumnsProvider : IDynamicColumnProvider
    {
        private readonly IDatabase _db;
        private readonly ICache _cache;

        private readonly IReadOnlyCollection<ColumnInfo> _columns = new List<ColumnInfo> 
        {
            new ColumnInfo(101, "Voc_Last30Days", "VOC za posledních 30 dnů"),
            new ColumnInfo(102, "Voc_LastYear", "VOC za minulý kalendářní rok"),
            new ColumnInfo(103, "Voc_ThisYear", "VOC za tento kalendářní rok"),
            new ColumnInfo(104, "Voc_Prev12M", "VOC za předchozích 12 měsíců (-24M až -12M)"),
            new ColumnInfo(105, "Voc_Last12M", "VOC za posledních 12 měsíců "),
            new ColumnInfo(106, "Voc_LastQuarter", "VOC za minulý kvartál"),
            new ColumnInfo(107, "Voc_ThisQuarter", "VOC za aktuální kvartál"),
            new ColumnInfo(108, "Voc_Prev3M", "VOC za předchozí 3 měsíce (-6M až -3M)"),
            new ColumnInfo(109, "Voc_Last3M", "VOC za poslední 3 měsíce"),
            new ColumnInfo(110, "Voc_Prev6M", "VOC za předchozích 6 měsíců (-12M až -6M)"),
            new ColumnInfo(111, "Voc_Last6M", "VOC za posledních 6 měsíců"),
            new ColumnInfo(112, "Voc_ThisVsLastYear_Pct", "VOC % tohoto roku oproti minulému"),
            new ColumnInfo(113, "Voc_Last12VsPrev12_Pct", "VOC % posledních 12M oproti předchozím 12M")
        };

        private readonly Dictionary<string, ColumnInfo> _columnIndex;

        public SalesStatsColumnsProvider(IDatabase db, ICache cache)
        {
            _db = db;
            _cache = cache;

            _columnIndex = _columns.ToDictionary(c => c.Id, c => c);
        }

        public IReadOnlyCollection<ColumnInfo> GetAvailableColumns()
        {
            return _columns;
        }

        public void Populate(string columnId, List<DistributorGridRowModel> rows)
        {
            var data = GetData();

            foreach(var row in rows)
            {
                if (data.TryGetValue(row.Id, out var customerData) && customerData.TryGetValue(columnId, out var value))
                    row.DynamicColumns[columnId] = value;
                else
                    row.DynamicColumns[columnId] = 0;
            }
        }

        public void RenderCell(string columnId, Func<string, string> mapPath, StringBuilder sb)
        {
            sb.AppendLine("<div class=\"cell5\" data-bind=\"text:DynamicColumns.{columnId}\"></div>");
        }

        public void RenderHead(string columnId, Func<string, string> mapPath, StringBuilder sb)
        {
            sb.AppendLine($"<div class=\"cell5\">{_columnIndex[columnId].Title}</div>");
        }

        private Dictionary<int, Dictionary<string, int>> GetData()
        {
            return _cache.ReadThrough("crmSalesStats",
                TimeSpan.FromHours(1),
                () => {
                var result = new Dictionary<int, Dictionary<string, int>>(30000);

                _db.Sql().Call("CrmLoadSalesStats")
                .ReadRows(rowReader => {

                    Dictionary<string, int> rowData = null;

                    // 1 because 1st col = CustomerId
                    for (var ordinal = 1; ordinal <= _columns.Count; ordinal++) 
                    {
                        var value = rowReader.GetInt32(ordinal);
                        if (value == 0)
                            continue;

                        var name = rowReader.GetName(ordinal);
                        
                        if(rowData == null)
                        {
                            rowData = new Dictionary<string, int>();
                            rowData.Add(name, value);
                            result.Add(rowReader.GetInt32(0), rowData);
                        }
                    }
                });

                return result;
            });
        }
    }
}
