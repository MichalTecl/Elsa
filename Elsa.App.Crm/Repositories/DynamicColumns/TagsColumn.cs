using Elsa.App.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class TagsColumn : DynamicColumnBase
    {
        public override int DisplayOrder => 10;
        public override string Id => "Tags";

        public override string Title => "Štítky";

        public override string BoundProperty => "tags";

        public override string CellClass => "cell20";

        public override Task PopulateAsync(List<DistributorGridRowModel> rows)
        {
            return Task.CompletedTask;
        }

        public override string GetCellControl(string columnId, string cellClass, string boundProperty)
        {
            return @"<div class=""cell20 digrTagsCell"">
                        <div data-bind=""itemsSource:tags"" data-key=""Id"" class=""digrCustTagsContainer stackLeft"">
                            <div class=""lt-template digrCustTagItem"" data-bind=""text:Name;cssClass:CssClass""></div>
                        </div>
                    </div>";
        }
    }
}
