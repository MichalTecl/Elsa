using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using RobOrm.Core.Query.Abstraction;
using RobOrm.Core.Query.Filtering;
using RobOrm.Core.Query.Model;

namespace RobOrm.Core.Query.Building
{
    public class QueryModel<T> : IQueryModel<T>, IHasBuilder<T> where T : class
    {
        private readonly IQueryBuilder<T> _owner;

        public QueryModel(string rootTableAlias, string rootTableName, IEnumerable<SelectedColumnModel> selectedColumns, IEnumerable<JoinModel> joins, Expression<Func<T, bool>> where, IQueryBuilder<T> owner, int? take, int? skip, IEnumerable<ResultOrderingModel> orderBy)
        {
            RootTableAlias = rootTableAlias;
            RootTableName = rootTableName;
            SelectedColumns = selectedColumns;
            Joins = joins;
            Where = @where;
            _owner = owner;
            Take = take;
            Skip = skip;
            OrderBy = orderBy;
        }

        public IQueryBuilder<T> OwnerBuilder => _owner;

        public string RootTableAlias { get; }

        public string RootTableName { get; }

        public int? Take { get; }

        public int? Skip { get; }

        public IEnumerable<SelectedColumnModel> SelectedColumns { get; }

        public IEnumerable<JoinModel> Joins { get; }

        public Expression<Func<T, bool>> Where { get; }

        public IEnumerable<ResultOrderingModel> OrderBy { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("SELECT ");

            var firstColumn = true;

            foreach (var column in SelectedColumns)
            {
                if (!firstColumn)
                {
                    sb.Append(", ");
                }
                firstColumn = false;

                sb.Append(column);
            }

            sb.AppendLine();

            sb.Append("FROM ").Append($"[{RootTableName}] AS ").Append($"[{RootTableAlias}]").AppendLine();

            foreach (var join in Joins)
            {
                sb.AppendLine(join.ToString());
            }

            var expComp = new ExpressionQueryBuilder<T>(_owner);
            var where = expComp.Map(Where);

            where.Render(sb);

            sb.AppendLine();

            ResultOrderingModel.Render<T>(OrderBy, sb);

            if (Skip != null)
            {
                sb.AppendLine($"OFFSET {Skip} ROWS");
            }

            if (Take != null)
            {
                sb.AppendLine($"FETCH NEXT {Take} ROWS ONLY");
            }

            return sb.ToString();
        }
    }
}
