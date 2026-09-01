using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Robowire.RobOrm.SqlServer.Aggregations
{
    internal static class AggregationExpressionParser
    {
        public static string GetSourceColumnAlias<TSource>(Expression<Func<TSource, object>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            return GetSourceColumnAlias(expression.Body, expression.Parameters.Single(), typeof(TSource));
        }

        public static string GetMatchingTargetPropertyName<TSource, TGroup>(
            Expression<Func<TSource, object>> sourceExpression)
        {
            if (sourceExpression == null)
                throw new ArgumentNullException(nameof(sourceExpression));

            var sourceProperty = RemoveConvert(sourceExpression.Body) as MemberExpression;
            if (!(sourceProperty?.Member is PropertyInfo))
                throw new ArgumentException("The source expression must select an entity property.", nameof(sourceExpression));

            var targetProperty = typeof(TGroup).GetProperty(sourceProperty.Member.Name);
            if (targetProperty == null || !targetProperty.CanWrite)
            {
                throw new ArgumentException(
                    $"The result type {typeof(TGroup).Name} must have a writable property named {sourceProperty.Member.Name}.",
                    nameof(sourceExpression));
            }

            return targetProperty.Name;
        }

        public static string GetTargetPropertyName<TGroup>(Expression<Func<TGroup, object>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var body = RemoveConvert(expression.Body) as MemberExpression;
            if (body == null || RemoveConvert(body.Expression) != expression.Parameters.Single())
                throw new ArgumentException("The target expression must select a direct result property.", nameof(expression));

            var property = body.Member as PropertyInfo;
            if (property == null || !property.CanWrite)
                throw new ArgumentException("The selected result member must be a writable property.", nameof(expression));

            return property.Name;
        }

        public static AggregateExpression GetAggregate<TSource, TValue>(Expression<Func<TSource, TValue>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var call = RemoveConvert(expression.Body) as MethodCallExpression;
            var function = call?.Method.GetCustomAttribute<AggregationFunctionAttribute>();
            if (call == null || function == null || call.Arguments.Count != 1)
                throw new ArgumentException("Bind accepts only RobOrm aggregation marker functions.", nameof(expression));

            var argument = RemoveConvert(call.Arguments[0]);

            if (argument == expression.Parameters.Single() && function.AllowWholeRow)
            {
                if (function.Distinct)
                    throw new ArgumentException("A whole-row aggregate cannot be distinct.", nameof(expression));

                return new AggregateExpression(function, null);
            }

            if (argument == expression.Parameters.Single())
            {
                throw new ArgumentException(
                    $"{call.Method.Name}() must select an entity property.",
                    nameof(expression));
            }

            if (function.NumericOnly && !IsNumeric(argument.Type))
            {
                throw new ArgumentException(
                    $"{call.Method.Name}() cannot be used with {argument.Type.Name}.",
                    nameof(expression));
            }

            return new AggregateExpression(
                function,
                GetSourceColumnAlias(argument, expression.Parameters.Single(), typeof(TSource)));
        }

        private static bool IsNumeric(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        private static string GetSourceColumnAlias(Expression expression, ParameterExpression parameter, Type sourceType)
        {
            var members = new Stack<string>();
            var current = RemoveConvert(expression);

            while (current is MemberExpression member)
            {
                if (!(member.Member is PropertyInfo))
                    throw new ArgumentException("Only mapped entity properties can be used in an aggregation.");

                members.Push(member.Member.Name);
                current = RemoveConvert(member.Expression);
            }

            if (current != parameter || members.Count == 0)
                throw new ArgumentException("The source expression must select an entity property.");

            return sourceType.Name + "." + string.Join(".", members);
        }

        private static Expression RemoveConvert(Expression expression)
        {
            while (expression != null
                   && (expression.NodeType == ExpressionType.Convert
                       || expression.NodeType == ExpressionType.ConvertChecked))
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            return expression;
        }
    }

    internal sealed class AggregateExpression
    {
        public AggregateExpression(AggregationFunctionAttribute function, string sourceColumnAlias)
        {
            Function = function;
            SourceColumnAlias = sourceColumnAlias;
        }

        public AggregationFunctionAttribute Function { get; }

        public string SourceColumnAlias { get; }
    }
}
