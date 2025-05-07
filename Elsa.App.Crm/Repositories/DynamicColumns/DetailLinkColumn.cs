using Elsa.App.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class DetailLinkColumn : DynamicColumnBase
    {
        public override string Id => "DetailLink";

        public override string Title => "Jméno";

        public override string BoundProperty => "Name";

        public override string CellClass => "cell10";

        public override Task PopulateAsync(List<DistributorGridRowModel> rows)
        {
            return Task.CompletedTask;
        }

        public override string GetCellControl(string columnId, string cellClass, string boundProperty)
        {
            return $"<div class=\"cell10 digrDetailLinkCell\">\r\n    <a data-bind=\"text:Name\" class=\"digrLinkSameWin\" event-bind=\"click:openDetail(Id)\"></a>\r\n    <a class=\"digrOutNewWin\" target=\"_blank\" data-bind=\"href:detailLink\"> <i class=\"fas fa-window-restore\"></i></a>\r\n</div>";
        }
    }
}
