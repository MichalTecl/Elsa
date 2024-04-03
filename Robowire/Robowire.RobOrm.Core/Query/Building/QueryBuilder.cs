using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using Robowire.Common.Expressions;
using Robowire.RobOrm.Core.Internal;
using Robowire.RobOrm.Core.Query.Abstraction;
using Robowire.RobOrm.Core.Query.Filtering;
using Robowire.RobOrm.Core.Query.Filtering.QuerySegments;
using Robowire.RobOrm.Core.Query.Filtering.QuerySegments.Misc;
using Robowire.RobOrm.Core.Query.Model;

namespace Robowire.RobOrm.Core.Query.Building
{
    public class QueryBuilder<T> : IQueryBuilder<T> where T:class
    {
        private static readonly Expression<Func<T, bool>> s_emptyWhere = t => true;

        private readonly IDataModelHelper m_dataModel;
        private readonly HashSet<string> m_inclusions = new HashSet<string>();
        private Expression<Func<T, bool>> m_where = s_emptyWhere;
        private readonly Dictionary<string, object> m_parameters = new Dictionary<string, object>();
        private readonly List<ResultOrderingModel> m_order = new List<ResultOrderingModel>();
        private readonly IDatabase m_owner;
        private readonly List<string> m_selectList = new List<string>();

        private int? m_skip;
        private int? m_take;
        
        public QueryBuilder(IDataModelHelper dataModel, IDatabase owner)
        {
            m_dataModel = dataModel;
            m_owner = owner;
        }

        public int ParametersCount => m_parameters.Count;

        public IQueryBuilder<T> Join<T2>(Expression<Func<T, T2>> expression)
        {
            var path = ExpressionsHelper.GetPropertiesChainText<T>(expression);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException($"Invalid join expression {expression}");
            }
            Include(path);
            
            return this;
        }

        public IQueryBuilder<T> Where(Expression<Func<T, bool>> condition)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            m_where = m_where == s_emptyWhere ? condition : ExpressionsHelper.CombineConditions(m_where, condition);


            return this;
        }

        public IQueryBuilder<T> OrderBy(Expression<Func<T, object>> expression)
        {
            m_order.Add(new ResultOrderingModel(true, expression));
            return this;
        }

        public IQueryBuilder<T> OrderByDesc(Expression<Func<T, object>> expression)
        {
            m_order.Add(new ResultOrderingModel(false, expression));
            return this;
        }

        public ITransformedQuery<TResult> Transform<TResult>(Expression<Func<T, TResult>> generator)
        {
            var expComp = new ExpressionQueryBuilder<T>(this);

            var seg = expComp.Map(generator) as WhereSegment;
            var selexp = seg.GetSelectExpression();

            if (!m_selectList.Contains(selexp))
            {
                m_selectList.Add(selexp);
            }

            return new TransformedQueryBuilder<T, TResult>(m_owner, this);
        }

        public IQueryModel<T> Build()
        {
            var selectedColumns = new List<SelectedColumnModel>();
            var joins = new List<JoinModel>();

            VisitEntity(typeof(T), typeof(T).Name, selectedColumns, joins);

            if (m_selectList.Any())
            {
                selectedColumns.Clear();

                foreach (var s in m_selectList)
                {
                    selectedColumns.Add(new SelectedColumnModel(null, null, s));
                }
            }

            var model = new QueryModel<T>(
                typeof(T).Name, 
                m_dataModel.GetTableName(typeof(T)), 
                selectedColumns, 
                joins, 
                m_where, 
                this,
                m_take,
                m_skip,
                m_order);

            return model;
        }

        public IEnumerable<T> Execute() 
        {
            var model = Build();
            return m_owner.Select(model);
        }

        public IQuerySegment ToSegment(ExpressionMapperBase<IQuerySegment> queryMapper, IHasParameters paramsTarget)
        {
            var model = Build();
            var queryText = m_owner.GetQueryText(model, this);

            foreach (var p in m_parameters)
            {
                paramsTarget.AddParameter(p.Key, p.Value);
            }

            return new SubquerySegment(queryText);
        }

        public IQueryBuilder<T> Take(int rowsCount)
        {
            m_take = rowsCount;
            return this;
        }

        public IQueryBuilder<T> Skip(int rowsCount)
        {
            m_skip = rowsCount;
            return this;
        }
        
        private void Include(string propertyPath)
        {
            propertyPath = DotPathHelper.TrimDots(propertyPath);

            while (propertyPath.Contains("."))
            {
                m_inclusions.Add(propertyPath);
                propertyPath = DotPathHelper.GetParent(propertyPath);
            }
        }

        private bool IsIncluded(string propertyPath)
        {
            propertyPath = DotPathHelper.TrimDots(propertyPath);
            return m_inclusions.Contains(propertyPath);
        }

        private void VisitEntity(Type entityType, string path, List<SelectedColumnModel> columns, List<JoinModel> joins)
        {
            foreach (var datacolumn in m_dataModel.GetTableDataColumns(entityType))
            {
                columns.Add(new SelectedColumnModel(path, m_dataModel.GetColumnName(datacolumn), null));
            }

            foreach (var reference in m_dataModel.GetReferences(entityType))
            {
                var propertyPath = $"{path}.{reference.LeftModelPropertyName}";
                if (!IsIncluded(propertyPath))
                {
                    continue;
                }

                var joinedTableAlias = DotPathHelper.Combine(path, reference.LeftModelPropertyName);

                var join = new JoinModel()
                               {
                                   JoinType = JoinType.Left,
                                   Condition = $"[{path}].[{reference.LeftKeyColumnName}]=[{joinedTableAlias}].[{reference.RightKeyColumnName}]",
                                   JoinedTableAlias = joinedTableAlias,
                                   JoinedTableName = m_dataModel.GetTableName(reference.RightEntityType)
                               };

                joins.Add(join);

                VisitEntity(reference.RightEntityType, joinedTableAlias, columns, joins);
            }
        }
        
        public void AddParameter(string name, object value)
        {
            m_parameters.Add(name, value);
        }

        public IEnumerable<KeyValuePair<string, object>> GetParameters()
        {
            return m_parameters;
        }

        private class SubquerySegment : IQuerySegment, IBooleanSegment
        {
            private readonly string m_queryText;

            public SubquerySegment(string queryText)
            {
                m_queryText = queryText;
            }

            public void Render(StringBuilder sb)
            {
                sb.Append("(");
                sb.Append(m_queryText);
                sb.Append(")");
            }

            public void RenderAsBoolean(StringBuilder sb)
            {
                sb.Append("(");
                Render(sb);
                sb.Append(" = 1)");
            }
        }
    }
}
