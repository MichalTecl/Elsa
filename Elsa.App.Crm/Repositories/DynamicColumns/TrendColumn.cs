using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class TrendColumn : SimpleDynamicColumnBase
    {
        public override int DisplayOrder => 50;
        public override string Id => "Trend";

        public override string Title => "Trend";

        public override string BoundProperty => "";

        public override string CellClass => "cell10";

        public override bool CanSort => false;

        public override void Populate(List<DistributorGridRowModel> rows)
        {
        }

        public override string GetCellControl(string columnId, string cellClass, string boundProperty, Func<string, string> loadTemplate)
        {
            return @"<div class=""cell10 digrTrendCell"">
    <div data-bind=""itemsSource:TrendModel"" data-key=""id"" class=""digrTrendView"">
        <div data-bind=""style.height:height;class.digrTrendItemEmpty:IsEmpty;title:symbol"" class=""lt-template digrTrendItem""></div>
    </div>
</div>";
        }

        protected override Func<DistributorGridRowModel, IComparable> GetSorter()
        {
            throw new NotSupportedException();
        }
    }
}
