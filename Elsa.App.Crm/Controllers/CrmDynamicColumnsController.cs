using Elsa.App.Crm.Repositories.DynamicColumns;
using Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmDynamicColumns")]
    public class CrmDynamicColumnsController : ElsaControllerBase
    {
        private readonly ColumnFactory _columnFactory;
        private readonly ICache _cache;

        public CrmDynamicColumnsController(IWebSession webSession, ILog log, ColumnFactory columnFactory, ICache cache) : base(webSession, log)
        {
            _columnFactory = columnFactory;
            _cache = cache;
        }

        public List<ColumnInfo> GetColumns()
        {
            return _columnFactory.GetAllDefinedColumns();
        }

        public HtmlResult GetGridHtml(string query)
        {
#if (!DEBUG)
            return _cache.ReadThrough($"dynCols_{query}", TimeSpan.FromHours(1), () =>
            {
#endif
                var colNames = (string.IsNullOrWhiteSpace(query) ? DetailLinkColumn.DetailLinkColumnName : query.Trim()).Split(',');
                var templatePath = MapPath("/UI/DistributorsApp/Parts/DistributorGridParts/GridControlTemplate.html");
                var template = File.ReadAllText(templatePath);

                var headSb = new StringBuilder();
                var cellsSb = new StringBuilder();

                foreach (var column in _columnFactory.GetColumns(colNames))
                {
                    column.RenderHead(MapPath, headSb);
                    column.RenderCell(MapPath, cellsSb);
                }

                var sb = new StringBuilder(template);
                sb.Replace("{headers}", headSb.ToString());
                sb.Replace("{cells}", cellsSb.ToString());

                return new HtmlResult(sb.ToString());
#if (!DEBUG)
        });        
#endif
        }
    }
}
