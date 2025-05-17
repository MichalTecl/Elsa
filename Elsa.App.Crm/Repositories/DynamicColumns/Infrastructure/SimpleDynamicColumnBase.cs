using Elsa.App.Crm.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure
{
    public abstract class SimpleDynamicColumnBase : IDynamicColumnProvider
    {
        public abstract int DisplayOrder { get; }
        public abstract string Id { get; }
        public abstract string Title { get; }
        public abstract string BoundProperty { get; }
        public abstract string CellClass { get; }
        public virtual bool CanSort { get; } = true;
        
        public virtual void RenderCell(Func<string, string> mapPath, StringBuilder sb)
        {
            string mapper(string columnControlName) 
            {
                if (!columnControlName.Contains("."))
                    columnControlName = $"{columnControlName}.html";

                if (!columnControlName.Contains("/"))
                    columnControlName = $"/UI/DistributorsApp/Parts/DistributorGridParts/DynamicColumns/{columnControlName}";

                var path = mapPath(columnControlName);

                return File.ReadAllText(path);
            }

            sb.AppendLine(GetCellControl(Id, CellClass, BoundProperty, mapper));
        }
                        
        public virtual string GetCellControl(string columnId, string cellClass, string boundProperty, Func<string, string> loadTemplate)
        {
            return $"<div class=\"{CellClass} digr{Id}Cell\" data-bind=\"text:{boundProperty}\"></div>";
        } 
                
        public abstract void Populate(List<DistributorGridRowModel> rows);
                
        public virtual void InjectModelScript(StringBuilder target, Func<string, string> loadTempate)
        {
        }

        public IReadOnlyCollection<ColumnInfo> GetAvailableColumns()
        {
            return new List<ColumnInfo> { 
                new ColumnInfo(DisplayOrder, Id, Title) }.AsReadOnly();
        }

        public virtual void RenderCell(string columnId, Func<string, string> mapPath, StringBuilder sb)
        {
            RenderCell(mapPath, sb);
        }

        public virtual void RenderHead(string columnId, Func<string, string> mapPath, StringBuilder sb)
        {
            ColumnHeadControlLoader.Render(columnId, Title, CellClass, CanSort, mapPath, sb);
        }

        public void Populate(string columnId, List<DistributorGridRowModel> rows, bool? sortDescending)
        {
            Populate(rows);

            if (sortDescending != null)
            {                
                rows.Sort(DistributorGridRowModel.GetComparer(GetSorter(), sortDescending.Value));
            }
        }

        protected abstract Func<DistributorGridRowModel, IComparable> GetSorter();

        
    }
}
