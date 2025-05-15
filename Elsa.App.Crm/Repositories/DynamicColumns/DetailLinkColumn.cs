using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class DetailLinkColumn : SimpleDynamicColumnBase
    {
        public override int DisplayOrder => 0;

        public static string DetailLinkColumnName = "DetailLink";

        public override string Id => DetailLinkColumnName;

        public override string Title => "Jméno";

        public override string BoundProperty => "Name";

        public override string CellClass => "cell10";

        public override string GetCellControl(string columnId, string cellClass, string boundProperty, Func<string, string> loadTemplate)
        {
            return $"<div class=\"cell10 digrDetailLinkCell\">\r\n    <a data-bind=\"text:Name\" class=\"digrLinkSameWin\" event-bind=\"click:openDetail(Id)\"></a>\r\n    <a class=\"digrOutNewWin\" target=\"_blank\" data-bind=\"href:detailLink\"> <i class=\"fas fa-window-restore\"></i></a>\r\n</div>";
        }

        public override void Populate(List<DistributorGridRowModel> rows)
        {            
        }
    }
}
