using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class TotalOrdersColumn : SimpleDynamicColumnBase
    {
        public override int DisplayOrder => 40;

        public override string Id => "TotalPrice";

        public override string Title => "Tržby celkem";

        public override string BoundProperty => "TotalOrdersPriceF";

        public override string CellClass => "cell5";

        public override void Populate(List<DistributorGridRowModel> rows)
        {
        }

        protected override Func<DistributorGridRowModel, IComparable> GetSorter()
        {
            return m => m.TotalOrdersUntaxedPrice;
        }
    }
}
