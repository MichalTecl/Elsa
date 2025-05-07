using Elsa.App.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class TotalOrdersColumn : DynamicColumnBase
    {
        public override string Id => "TotalPrice";

        public override string Title => "Tržby celkem";

        public override string BoundProperty => "TotalOrdersPriceF";

        public override string CellClass => "cell5";

        public override Task PopulateAsync(List<DistributorGridRowModel> rows)
        {
            return Task.CompletedTask;
        }
    }
}
