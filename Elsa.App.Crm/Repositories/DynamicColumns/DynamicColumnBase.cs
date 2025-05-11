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
        public abstract int DisplayOrder { get; }
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string BoundProperty { get; }
        public abstract string CellClass { get; }

        public virtual void RenderHead(StringBuilder sb)
        {
            sb.AppendLine(GetHeadControl(Id, CellClass, BoundProperty));
        }

        public virtual void RenderCell(StringBuilder sb)
        {
            sb.AppendLine(GetCellControl(Id, CellClass, BoundProperty));
        }
                        
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
