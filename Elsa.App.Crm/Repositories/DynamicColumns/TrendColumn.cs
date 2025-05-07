using Elsa.App.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class TrendColumn : DynamicColumnBase
    {
        public override string Id => "Trend";

        public override string Title => "Trend";

        public override string BoundProperty => "";

        public override string CellClass => "cell10";

        public override Task PopulateAsync(List<DistributorGridRowModel> rows)
        {
            return Task.CompletedTask;
        }

        public override string GetCellControl(string columnId, string cellClass, string boundProperty)
        {
            return @"<div class=""cell10 digrTrendCell"">
    <div data-bind=""itemsSource:TrendModel"" data-key=""id"" class=""digrTrendView"">
        <div data-bind=""style.height:height;class.digrTrendItemEmpty:IsEmpty;title:symbol"" class=""lt-template digrTrendItem""></div>
    </div>
</div>";
        }
    }
}
