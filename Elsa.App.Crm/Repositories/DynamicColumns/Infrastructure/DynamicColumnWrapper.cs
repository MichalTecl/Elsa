using Elsa.App.Crm.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure
{
    public sealed class DynamicColumnWrapper 
    {
        private readonly IDynamicColumnProvider _provider;

        public ColumnInfo Column { get; }

        public DynamicColumnWrapper(ColumnInfo column, IDynamicColumnProvider provider)
        {
            Column = column;
            _provider = provider;
        }

        public void Populate(List<DistributorGridRowModel> rows)
        {
            _provider.Populate(Column.Id, rows);
        }

        public void RenderCell(Func<string, string> mapPath, StringBuilder sb)
        {
            _provider.RenderCell(Column.Id, mapPath, sb);
        }

        public void RenderHead(Func<string, string> mapPath, StringBuilder sb)
        {
            _provider.RenderHead(Column.Id, mapPath, sb);
        }
    }
}
