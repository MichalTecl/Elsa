using Elsa.App.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{


    public abstract class DynamicColumnBase
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string BoundProperty { get; }
        public abstract string CellClass { get; }
                        
        public virtual string GetCellControl(string columnId, string cellClass, string boundProperty)
        {
            return $"<div class=\"{CellClass} digr{Id}Cell\" data-bind=\"text:{boundProperty}\"></div>";
        } 

        public virtual string GetHeadControl(string columnId, string cellClass, string boundProperty)
        {
            return $"<div class=\"{CellClass}\">{Title}</div>";
        }

        public abstract Task PopulateAsync(List<DistributorGridRowModel> rows);
    }
}
