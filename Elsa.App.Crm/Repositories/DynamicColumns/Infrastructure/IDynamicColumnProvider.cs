using Elsa.App.Crm.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Elsa.App.Crm.Repositories.DynamicColumns.ColumnFactory;

namespace Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure
{
    public interface IDynamicColumnProvider
    {
        IReadOnlyCollection<ColumnInfo> GetAvailableColumns();

        void RenderCell(string columnId, Func<string, string> mapPath, StringBuilder sb);

        void RenderHead(string columnId, Func<string, string> mapPath, StringBuilder sb);

        void Populate(string columnId, List<DistributorGridRowModel> rows);
    }
}

