using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

using RobOrm.Core.Expressions;
using RobOrm.Core.Internal;
using RobOrm.Core.Query.Abstraction;
using RobOrm.Core.Query.Filtering;
using RobOrm.Core.Query.Filtering.QuerySegments;
using RobOrm.Core.Query.Filtering.QuerySegments.Misc;
using RobOrm.Core.Query.Model;

namespace RobOrm.Core.Query.Building
{
    public class QueryBuilder<T> : IQueryBuilder<T> where T:class
    {
        private static readonly Expression<Func<T, bool>> s_emptyWhere = t => true;

        private readonly IDataModelHelper _dataModel;
        private readonly HashSet<string> _inclusions = new HashSet<string>();
        private Expression<Func<T, bool>> _where = s_emptyWhere;
        private readonly Dictionary<string, object> _parameters = new Dictionary<string, object>();
        private readonly List<ResultOrderingModel> _order = new List<ResultOrderingModel>();
        private readonly IDatabase _owner;
        private readonly List<string> _selectList = new List<string>();

        private int? _skip;
        private int? _take;
        
        public QueryBuilder(IDataModelHelper dataModel, IDatabase owner)
        {
            _dataModel = dataModel;
            _owner = owner;
        }

        public int ParametersCount => _parameters.Count;

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

            _where = _where == s_emptyWhere ? condition : ExpressionsHelper.CombineConditions(_where, condition);


            return this;
        }

        public IQueryBuilder<T> OrderBy(Expression<Func<T, object>> expression)
        {
            _order.Add(new ResultOrderingModel(true, expression));
            return this;
        }

        public IQueryBuilder<T> OrderByDesc(Expression<Func<T, object>> expression)
        {
            _order.Add(new ResultOrderingModel(false, expression));
            return this;
        }

        public ITransformedQuery<TResult> Transform<TResult>(Expression<Func<T, TResult>> generator)
        {
            var expComp = new ExpressionQueryBuilder<T>(this);

            var seg = expComp.Map(generator) as WhereSegment;
            var selexp = seg.GetSelectExpression();

            if (!_selectList.Contains(selexp))
            {
                _selectList.Add(selexp);
            }

            return new TransformedQueryBuilder<T, TResult>(_owner, this);
        }

        public IQueryModel<T> Build()
        {
            var selectedColumns = new List<SelectedColumnModel>();
            var joins = new List<JoinModel>();

            VisitEntity(typeof(T), typeof(T).Name, selectedColumns, joins);

            if (_selectList.Any())
            {
                selectedColumns.Clear();

                foreach (var s in _selectList)
                {
                    selectedColumns.Add(new SelectedColumnModel(null, null, s));
                }
            }

            var model = new QueryModel<T>(
                typeof(T).Name, 
                _dataModel.GetTableName(typeof(T)), 
                selectedColumns, 
                joins, 
                _where, 
                this,
                _take,
                _skip,
                _order);

            return model;
        }

        public IEnumerable<T> Execute() 
        {
            var model = Build();
            return _owner.Select(model);
        }

        public IQuerySegment ToSegment(ExpressionMapperBase<IQuerySegment> queryMapper, IHasParameters paramsTarget)
        {
            var model = Build();
            var queryText = _owner.GetQueryText(model, this);

            if (_parameters.Count > 0)
            {
                var targetParameterNames = new HashSet<string>(
                    paramsTarget.GetParameters().Select(parameter => parameter.Key),
                    StringComparer.Ordinal);

                var scopeNumber = 1;
                string parameterPrefix;
                do
                {
                    parameterPrefix = $"@sq{scopeNumber++}_";
                }
                while (targetParameterNames.Any(name => name.StartsWith(parameterPrefix, StringComparison.Ordinal)));

                var parameterMappings = new Dictionary<string, string>(StringComparer.Ordinal);
                var parameterNumber = 1;

                foreach (var parameter in _parameters.OrderBy(item => item.Key, StringComparer.Ordinal))
                {
                    var scopedName = $"{parameterPrefix}p{parameterNumber++}";
                    parameterMappings.Add(parameter.Key, scopedName);
                    paramsTarget.AddParameter(scopedName, parameter.Value);
                }

                var parameterPattern = string.Join(
                    "|",
                    parameterMappings.Keys
                        .OrderByDescending(name => name.Length)
                        .Select(Regex.Escape));

                queryText = Regex.Replace(
                    queryText,
                    $"(?:{parameterPattern})(?![A-Za-z0-9_])",
                    match => parameterMappings[match.Value]);
            }

            return new SubquerySegment(queryText);
        }

        public IQueryBuilder<T> Take(int rowsCount)
        {
            _take = rowsCount;
            return this;
        }

        public IQueryBuilder<T> Skip(int rowsCount)
        {
            _skip = rowsCount;
            return this;
        }
        
        private void Include(string propertyPath)
        {
            propertyPath = DotPathHelper.TrimDots(propertyPath);

            while (propertyPath.Contains("."))
            {
                _inclusions.Add(propertyPath);
                propertyPath = DotPathHelper.GetParent(propertyPath);
            }
        }

        private bool IsIncluded(string propertyPath)
        {
            propertyPath = DotPathHelper.TrimDots(propertyPath);
            return _inclusions.Contains(propertyPath);
        }

        private void VisitEntity(Type entityType, string path, List<SelectedColumnModel> columns, List<JoinModel> joins)
        {
            foreach (var datacolumn in _dataModel.GetTableDataColumns(entityType))
            {
                columns.Add(new SelectedColumnModel(path, _dataModel.GetColumnName(datacolumn), null));
            }

            foreach (var reference in _dataModel.GetReferences(entityType))
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
                                   JoinedTableName = _dataModel.GetTableName(reference.RightEntityType)
                               };

                joins.Add(join);

                VisitEntity(reference.RightEntityType, joinedTableAlias, columns, joins);
            }
        }
        
        public void AddParameter(string name, object value)
        {
            _parameters.Add(name, value);
        }

        public IEnumerable<KeyValuePair<string, object>> GetParameters()
        {
            return _parameters;
        }

        private class SubquerySegment : IQuerySegment, IBooleanSegment
        {
            private readonly string _queryText;

            public SubquerySegment(string queryText)
            {
                _queryText = queryText;
            }

            public void Render(StringBuilder sb)
            {
                sb.Append("(");
                sb.Append(_queryText);
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
