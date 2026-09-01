using System;
using System.Linq.Expressions;
using Robowire.RobOrm.Core;

namespace Robowire.RobOrm.SqlServer.Aggregations
{
    public static class AggregationQueryExtensions
    {
        public static AggregationQueryBuilder<TSource> AggregateFrom<TSource>(this IDatabase database)
            where TSource : class
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            return new AggregationQueryBuilder<TSource>(database);
        }
    }

    public sealed class AggregationQueryBuilder<TSource>
        where TSource : class
    {
        private readonly IDatabase _database;
        private readonly IQueryBuilder<TSource> _sourceQuery;

        internal AggregationQueryBuilder(IDatabase database)
        {
            _database = database;
            _sourceQuery = database.SelectFrom<TSource>();
        }

        public AggregationQueryBuilder<TSource> Where(Expression<Func<TSource, bool>> condition)
        {
            _sourceQuery.Where(condition);
            return this;
        }

        public AggregationQueryBuilder<TSource> Join<TJoined>(Expression<Func<TSource, TJoined>> expression)
        {
            _sourceQuery.Join(expression);
            return this;
        }

        public AggregationQueryBuilder<TSource> Apply(Action<IQueryBuilder<TSource>> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            configure(_sourceQuery);
            return this;
        }

        public GroupedAggregationQueryBuilder<TSource, TGroup> GroupBy<TGroup>(
            Expression<Func<TSource, object>> sourceProperty)
            where TGroup : class, new()
        {
            var targetPropertyName =
                AggregationExpressionParser.GetMatchingTargetPropertyName<TSource, TGroup>(sourceProperty);

            return new GroupedAggregationQueryBuilder<TSource, TGroup>(_database, _sourceQuery)
                .GroupBy(sourceProperty, targetPropertyName);
        }

        public GroupedAggregationQueryBuilder<TSource, TGroup> GroupBy<TGroup>(
            Expression<Func<TSource, object>> sourceProperty,
            Expression<Func<TGroup, object>> resultProperty)
            where TGroup : class, new()
        {
            return new GroupedAggregationQueryBuilder<TSource, TGroup>(_database, _sourceQuery)
                .GroupBy(sourceProperty, resultProperty);
        }

        public GroupedAggregationQueryBuilder<TSource, TGroup> GroupAll<TGroup>()
            where TGroup : class, new()
        {
            return new GroupedAggregationQueryBuilder<TSource, TGroup>(_database, _sourceQuery);
        }
    }
}
