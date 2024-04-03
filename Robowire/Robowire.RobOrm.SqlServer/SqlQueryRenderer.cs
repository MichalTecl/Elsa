using System.Text;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.Core.Query.Abstraction;
using Robowire.RobOrm.Core.Query.Filtering;
using Robowire.RobOrm.Core.Query.Model;

namespace Robowire.RobOrm.SqlServer
{
    public static class SqlQueryRenderer
    {
        public static string Render<T>(IQueryModel<T> query, IQueryBuilder<T> queryBuilder) where T:class
        {
            var sb = new StringBuilder();

            var takeRendered = false;

            sb.Append("SELECT ");

            if ((query.Skip == null) && (query.Take != null))
            {
                sb.Append($"TOP {query.Take} ");
                takeRendered = true;
            }

            var firstColumn = true;

            foreach (var column in query.SelectedColumns)
            {
                if (!firstColumn)
                {
                    sb.Append(", ");
                }
                firstColumn = false;

                sb.Append(column);
            }

            sb.AppendLine();

            sb.Append("FROM ").Append($"[{query.RootTableName}] AS ").Append($"[{query.RootTableAlias}]").AppendLine();

            foreach (var join in query.Joins)
            {
                sb.AppendLine(join.ToString());
            }

            var expComp = new ExpressionQueryBuilder<T>(queryBuilder);
            var where = expComp.Map(query.Where);

            where.Render(sb);

            sb.AppendLine();

            ResultOrderingModel.Render<T>(query.OrderBy, sb);

            
            if(!takeRendered)
            {
                if (query.Skip != null)
                {
                    sb.AppendLine($"OFFSET {query.Skip} ROWS");
                }

                if (query.Take != null)
                {
                    sb.AppendLine($"FETCH NEXT {query.Take} ROWS ONLY");
                }
            }

            return sb.ToString();
        }
    }
}
