using Elsa.App.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class CategoriesColumn : DynamicColumnBase
    {
        public override string Id => "Groups";

        public override string Title => "Kategorie";

        public override string BoundProperty => "";

        public override string CellClass => "cell10";

        public override Task PopulateAsync(List<DistributorGridRowModel> rows)
        {
            return Task.CompletedTask;
        }

        public override string GetCellControl(string columnId, string cellClass, string boundProperty)
        {
            return @"<div class=""cell10 digrGroupsCell"">
                        <div data-bind=""itemsSource:customerGroups"" data-key=""Id"" class=""digrCustGroupsContainer stackLeft"">
                            <div class=""lt-template digrCustGroupItem"" data-bind=""text:ErpGroupName""></div>
                        </div>
                    </div>";
        }
    }
}
