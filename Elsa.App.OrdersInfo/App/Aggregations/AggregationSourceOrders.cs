using Elsa.Core.Entities.Commerce.Commerce;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.Core.NonOrm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.App.OrdersInfo.App.Aggregations
{
    /// <summary>
    /// Source orders of an aggregation (orders matching the user's filter) prepared for use in a raw SQL.
    /// The aggregation SQL refers to them by placeholder, e.g. "WHERE po.Id IN ({sourceOrdersQuery})".
    /// Parameters of the source query are renamed to a separate scope (@sq1_p1, ...), so the aggregation SQL
    /// can use its own parameters without collisions.
    /// </summary>
    public class AggregationSourceOrders
    {
        public const string SOURCE_ORDERS_QUERY_PLACEHOLDER = "{sourceOrdersQuery}";

        private readonly IDatabase _db;

        public AggregationSourceOrders(IDatabase db, IQueryBuilder<IPurchaseOrder> sourceOrdersQuery)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));

            if (sourceOrdersQuery == null)
                throw new ArgumentNullException(nameof(sourceOrdersQuery));

            sourceOrdersQuery.Transform(order => order.Id);

            var parameters = new ParametersCollector();
            var segment = sourceOrdersQuery.ToSegment(null, parameters);

            var sb = new StringBuilder();
            segment.Render(sb);

            OrderIdsSql = TrimOuterParentheses(sb.ToString());
            Parameters = parameters.GetParameters().ToList();
        }

        /// <summary>
        /// SQL returning a single column with Ids of the source orders (without enclosing parentheses)
        /// </summary>
        public string OrderIdsSql { get; }

        /// <summary>
        /// Scoped parameters required by <see cref="OrderIdsSql"/>
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, object>> Parameters { get; }

        /// <summary>
        /// Creates a command from SQL containing the {sourceOrdersQuery} placeholder.
        /// Own parameters can be passed the same way as to ISqlBuilder.ExecuteWithParams ({0}, {1}, ...)
        /// or added later by WithParam on the returned executor.
        /// </summary>
        public ISqlExecutor Sql(string sql, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentNullException(nameof(sql));

            if (!sql.Contains(SOURCE_ORDERS_QUERY_PLACEHOLDER))
                throw new ArgumentException($"SQL agregace musí obsahovat placeholder {SOURCE_ORDERS_QUERY_PLACEHOLDER}");

            sql = sql.Replace(SOURCE_ORDERS_QUERY_PLACEHOLDER, OrderIdsSql);

            var executor = parameters?.Length > 0
                ? _db.Sql().ExecuteWithParams(sql, parameters)
                : _db.Sql().Execute(sql);

            foreach (var parameter in Parameters)
                executor.WithParam(parameter.Key, parameter.Value);

            return executor;
        }

        private static string TrimOuterParentheses(string sql)
        {
            sql = sql.Trim();

            if (sql.StartsWith("(") && sql.EndsWith(")"))
                sql = sql.Substring(1, sql.Length - 2);

            return sql;
        }

        private class ParametersCollector : IHasParameters
        {
            private readonly List<KeyValuePair<string, object>> _parameters = new List<KeyValuePair<string, object>>();

            public int ParametersCount => _parameters.Count;

            public void AddParameter(string name, object value)
            {
                _parameters.Add(new KeyValuePair<string, object>(name, value));
            }

            public IEnumerable<KeyValuePair<string, object>> GetParameters()
            {
                return _parameters;
            }
        }
    }
}
