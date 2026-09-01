using System;

namespace Robowire.RobOrm.SqlServer.Aggregations
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class AggregationFunctionAttribute : Attribute
    {
        public AggregationFunctionAttribute(string sqlFunctionName)
        {
            if (string.IsNullOrWhiteSpace(sqlFunctionName))
                throw new ArgumentException("SQL function name must be specified.", nameof(sqlFunctionName));

            foreach (var character in sqlFunctionName)
            {
                if (!char.IsLetterOrDigit(character) && character != '_')
                {
                    throw new ArgumentException(
                        "SQL function name may contain only letters, digits and underscores.",
                        nameof(sqlFunctionName));
                }
            }

            SqlFunctionName = sqlFunctionName;
        }

        public string SqlFunctionName { get; }

        public bool AllowWholeRow { get; set; }

        public bool Distinct { get; set; }

        public bool NumericOnly { get; set; }

        public bool CoalesceZero { get; set; }
    }

    /// <summary>
    /// Marker functions interpreted by <see cref="GroupedAggregationQueryBuilder{TSource,TGroup}"/>.
    /// They may only be used inside an aggregation Bind expression.
    /// </summary>
    public static class AggregationFunctions
    {
        [AggregationFunction("COUNT", AllowWholeRow = true)]
        public static int Count<T>(this T value)
        {
            throw new InvalidOperationException("Count() is an aggregation marker and cannot be executed directly.");
        }

        [AggregationFunction("COUNT", Distinct = true)]
        public static int CountDistinct<T>(this T value)
        {
            throw new InvalidOperationException("CountDistinct() is an aggregation marker and cannot be executed directly.");
        }

        [AggregationFunction("COUNT_BIG", AllowWholeRow = true)]
        public static long LongCount<T>(this T value)
        {
            throw new InvalidOperationException("LongCount() is an aggregation marker and cannot be executed directly.");
        }

        [AggregationFunction("COUNT_BIG", Distinct = true)]
        public static long LongCountDistinct<T>(this T value)
        {
            throw new InvalidOperationException("LongCountDistinct() is an aggregation marker and cannot be executed directly.");
        }

        [AggregationFunction("SUM", NumericOnly = true, CoalesceZero = true)]
        public static T Sum<T>(this T value)
        {
            throw new InvalidOperationException("Sum() is an aggregation marker and cannot be executed directly.");
        }

        [AggregationFunction("AVG", NumericOnly = true)]
        public static T Average<T>(this T value)
        {
            throw new InvalidOperationException("Average() is an aggregation marker and cannot be executed directly.");
        }

        [AggregationFunction("AVG", NumericOnly = true)]
        public static T Avg<T>(this T value)
        {
            throw new InvalidOperationException("Avg() is an aggregation marker and cannot be executed directly.");
        }

        [AggregationFunction("MIN")]
        public static T Min<T>(this T value)
        {
            throw new InvalidOperationException("Min() is an aggregation marker and cannot be executed directly.");
        }

        [AggregationFunction("MAX")]
        public static T Max<T>(this T value)
        {
            throw new InvalidOperationException("Max() is an aggregation marker and cannot be executed directly.");
        }
    }
}
