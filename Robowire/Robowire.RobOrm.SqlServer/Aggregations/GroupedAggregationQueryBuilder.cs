using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Dapper;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.Core.Query.Abstraction;
using Robowire.RobOrm.Core.Query.Model;

namespace Robowire.RobOrm.SqlServer.Aggregations
{
    public sealed class GroupedAggregationQueryBuilder<TSource, TGroup>
        where TSource : class
        where TGroup : class, new()
    {
        private const string SOURCE_ALIAS = "RobOrmAggregateSource";

        private readonly IDatabase _database;
        private readonly IQueryBuilder<TSource> _sourceQuery;
        private readonly List<GroupBinding> _groups = new List<GroupBinding>();
        private readonly List<IAggregateBinding> _aggregates = new List<IAggregateBinding>();
        private readonly HashSet<string> _targetProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal GroupedAggregationQueryBuilder(IDatabase database, IQueryBuilder<TSource> sourceQuery)
        {
            _database = database;
            _sourceQuery = sourceQuery;
        }

        public GroupedAggregationQueryBuilder<TSource, TGroup> GroupBy(
            Expression<Func<TSource, object>> sourceProperty,
            Expression<Func<TGroup, object>> resultProperty)
        {
            var sourceColumnAlias = AggregationExpressionParser.GetSourceColumnAlias(sourceProperty);
            var targetPropertyName = AggregationExpressionParser.GetTargetPropertyName(resultProperty);
            AddTargetProperty(targetPropertyName);
            _groups.Add(new GroupBinding(sourceColumnAlias, targetPropertyName));
            return this;
        }

        internal GroupedAggregationQueryBuilder<TSource, TGroup> GroupBy(
            Expression<Func<TSource, object>> sourceProperty,
            string targetPropertyName)
        {
            var sourceColumnAlias = AggregationExpressionParser.GetSourceColumnAlias(sourceProperty);
            AddTargetProperty(targetPropertyName);
            _groups.Add(new GroupBinding(sourceColumnAlias, targetPropertyName));
            return this;
        }

        public GroupedAggregationQueryBuilder<TSource, TGroup> Bind<TValue>(
            Expression<Func<TSource, TValue>> aggregate,
            Action<TGroup, TValue> resultSetter)
        {
            if (resultSetter == null)
                throw new ArgumentNullException(nameof(resultSetter));

            var aggregateExpression = AggregationExpressionParser.GetAggregate(aggregate);
            var columnAlias = $"__RobOrmAggregate{_aggregates.Count}";
            _aggregates.Add(new AggregateBinding<TValue>(aggregateExpression, columnAlias, resultSetter));
            return this;
        }

        public List<TGroup> Execute()
        {
            if (_aggregates.Count == 0)
                throw new InvalidOperationException("At least one aggregate Bind must be specified.");

            var sourceModel = _sourceQuery.Build();
            ValidateSourceQuery(sourceModel);
            ValidateSelectedColumns(sourceModel);

            var sourceSql = _database.GetQueryText(sourceModel, _sourceQuery);
            var sql = BuildSql(sourceSql);
            var executor = _database.Sql().Execute(sql);

            foreach (var parameter in _sourceQuery.GetParameters())
                executor.WithParam(parameter.Key, parameter.Value);

            return executor.Read(reader => ReadResults(reader));
        }

        private List<TGroup> ReadResults(DbDataReader reader)
        {
            var result = new List<TGroup>();
            var groupMapper = reader.GetRowParser<TGroup>(typeof(TGroup));
            var aggregateOrdinals = _aggregates
                .Select(aggregate => reader.GetOrdinal(aggregate.ColumnAlias))
                .ToArray();

            while (reader.Read())
            {
                var group = groupMapper(reader);

                for (var index = 0; index < _aggregates.Count; index++)
                    _aggregates[index].Apply(group, reader, aggregateOrdinals[index]);

                result.Add(group);
            }

            return result;
        }

        private string BuildSql(string sourceSql)
        {
            var projections = new List<string>();
            projections.AddRange(_groups.Select(group =>
                $"{SourceColumn(group.SourceColumnAlias)} AS {QuoteIdentifier(group.TargetPropertyName)}"));
            projections.AddRange(_aggregates.Select(aggregate =>
                $"{RenderAggregate(aggregate.Expression)} AS {QuoteIdentifier(aggregate.ColumnAlias)}"));

            var sql = new StringBuilder();
            sql.Append("SELECT ").AppendLine(string.Join(", ", projections));
            sql.AppendLine("FROM (");
            sql.AppendLine(sourceSql);
            sql.Append(") AS ").AppendLine(QuoteIdentifier(SOURCE_ALIAS));

            if (_groups.Count > 0)
            {
                sql.Append("GROUP BY ").AppendLine(string.Join(", ",
                    _groups.Select(group => SourceColumn(group.SourceColumnAlias))));
            }

            return sql.ToString();
        }

        private static string RenderAggregate(AggregateExpression expression)
        {
            var argument = expression.SourceColumnAlias == null
                ? "1"
                : SourceColumn(expression.SourceColumnAlias);

            if (expression.Function.Distinct)
                argument = "DISTINCT " + argument;

            var aggregate = $"{expression.Function.SqlFunctionName}({argument})";
            return expression.Function.CoalesceZero
                ? $"COALESCE({aggregate}, 0)"
                : aggregate;
        }

        private static string SourceColumn(string columnAlias)
        {
            return $"{QuoteIdentifier(SOURCE_ALIAS)}.{QuoteIdentifier(columnAlias)}";
        }

        private static string QuoteIdentifier(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        private void AddTargetProperty(string propertyName)
        {
            if (!_targetProperties.Add(propertyName))
                throw new InvalidOperationException($"Result property {propertyName} is bound more than once.");
        }

        private static void ValidateSourceQuery(IQueryModel<TSource> sourceModel)
        {
            if (sourceModel.Take != null || sourceModel.Skip != null || sourceModel.OrderBy.Any())
                throw new InvalidOperationException("An aggregation source cannot contain paging or ordering.");
        }

        private void ValidateSelectedColumns(IQueryModel<TSource> sourceModel)
        {
            var selectedAliases = new HashSet<string>(
                sourceModel.SelectedColumns.Select(GetSelectedColumnAlias),
                StringComparer.OrdinalIgnoreCase);

            var requiredAliases = _groups.Select(group => group.SourceColumnAlias)
                .Concat(_aggregates
                    .Select(aggregate => aggregate.Expression.SourceColumnAlias)
                    .Where(alias => alias != null));

            var missingAlias = requiredAliases.FirstOrDefault(alias => !selectedAliases.Contains(alias));
            if (missingAlias != null)
            {
                throw new InvalidOperationException(
                    $"Column {missingAlias} is not selected by the source query. Add the required Join and do not Transform the aggregation source.");
            }
        }

        private static string GetSelectedColumnAlias(SelectedColumnModel column)
        {
            if (string.IsNullOrWhiteSpace(column.DirectExpression))
                return column.ColumnAlias;

            return column.DirectExpression.Replace("[", string.Empty).Replace("]", string.Empty);
        }

        private sealed class GroupBinding
        {
            public GroupBinding(string sourceColumnAlias, string targetPropertyName)
            {
                SourceColumnAlias = sourceColumnAlias;
                TargetPropertyName = targetPropertyName;
            }

            public string SourceColumnAlias { get; }

            public string TargetPropertyName { get; }
        }

        private interface IAggregateBinding
        {
            AggregateExpression Expression { get; }

            string ColumnAlias { get; }

            void Apply(TGroup group, DbDataReader reader, int ordinal);
        }

        private sealed class AggregateBinding<TValue> : IAggregateBinding
        {
            private readonly Action<TGroup, TValue> _resultSetter;

            public AggregateBinding(
                AggregateExpression expression,
                string columnAlias,
                Action<TGroup, TValue> resultSetter)
            {
                Expression = expression;
                ColumnAlias = columnAlias;
                _resultSetter = resultSetter;
            }

            public AggregateExpression Expression { get; }

            public string ColumnAlias { get; }

            public void Apply(TGroup group, DbDataReader reader, int ordinal)
            {
                TValue value;
                if (reader.IsDBNull(ordinal))
                {
                    if (typeof(TValue).IsValueType && Nullable.GetUnderlyingType(typeof(TValue)) == null)
                    {
                        throw new InvalidOperationException(
                            $"Aggregate {Expression.Function.SqlFunctionName} returned NULL. Use a nullable result value.");
                    }

                    value = default(TValue);
                }
                else
                {
                    value = reader.GetFieldValue<TValue>(ordinal);
                }

                _resultSetter(group, value);
            }
        }
    }
}
