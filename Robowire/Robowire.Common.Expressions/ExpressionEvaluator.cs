using System;
using System.Linq.Expressions;

namespace Robowire.Common.Expressions
{
    public static class ExpressionEvaluator
    {
        private static readonly ConstantExpressionChecker s_constantExpressionChecker = new ConstantExpressionChecker();

        public static object Eval(Expression expression)
        {
            var lambda = expression as LambdaExpression;
            if (lambda != null)
            {
                expression = lambda.Body;
            }

            expression = Expression.Convert(expression, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(expression);
            var getter = getterLambda.Compile();
            var value = getter();

            return value;
        }

        public static bool IsSelfEvaluable(Expression e)
        {
            return s_constantExpressionChecker.Map(e);
        }

        private class ConstantExpressionChecker : ExpressionMapperBase<bool>
        {
            protected override bool MapNullExpression()
            {
                throw new InvalidOperationException("Unsupported expression");
            }

            protected override bool MapExpression(Expression expression)
            {
                return false;
            }

            protected override bool MapBinaryExpression(BinaryExpression expression)
            {
                return Map(expression.Left) && Map(expression.Right);
            }

            protected override bool MapConditionalExpression(ConditionalExpression expression)
            {
                return Map(expression.IfFalse) && Map(expression.IfTrue) && Map(expression.Test);
            }

            protected override bool MapConstantExpression(ConstantExpression expression)
            {
                return true;
            }

            protected override bool MapBlockExpression(BlockExpression expression)
            {
                throw new NotSupportedException("Block expressions are not supported");
            }

            protected override bool MapInvocationExpression(InvocationExpression expression)
            {
                foreach (var arg in expression.Arguments)
                {
                    if (!Map(arg))
                    {
                        return false;
                    }
                }

                if (expression.Expression == null)
                {
                    return true;
                }

                return Map(expression.Expression);
            }

            protected override bool MapMemberExpression(MemberExpression expression)
            {
                return Map(expression.Expression);
            }

            protected override bool MapMethodCallExpression(MethodCallExpression expression)
            {
                foreach (var arg in expression.Arguments)
                {
                    if (!Map(arg))
                    {
                        return false;
                    }
                }

                return (expression.Object == null) || Map(expression.Object);
            }

            protected override bool MapParameterExpression(ParameterExpression expression)
            {
                return false;
            }

            protected override bool MapUnaryExpression(UnaryExpression expression)
            {
                return Map(expression.Operand);
            }
        }
    }
}
