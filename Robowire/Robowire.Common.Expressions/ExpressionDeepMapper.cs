using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Robowire.Common.Expressions
{
    internal class ExpressionDeepMapper : ExpressionMapperBase<IEnumerable<Expression>>
    {
        public override IEnumerable<Expression> Map(Expression expression)
        {
            if (expression == null)
            {
                yield break;
            }

            foreach (var e in base.Map(expression).Where(i => i != null))
            {
                yield return e;
            }
        }

        protected override IEnumerable<Expression> MapBinaryExpression(BinaryExpression expression)
        {
            return Unwrap(expression, expression.Left, expression.Right, expression.Conversion);
        }

        protected override IEnumerable<Expression> MapBlockExpression(BlockExpression expression)
        {
            return Unwrap(expression, expression.Expressions, expression.Variables, expression.Result);
        }

        protected override IEnumerable<Expression> MapConditionalExpression(ConditionalExpression expression)
        {
            return Unwrap(expression, expression.IfFalse, expression.IfTrue, expression.Test);
        }

        protected override IEnumerable<Expression> MapConstantExpression(ConstantExpression expression)
        {
            yield return expression;
        }

        protected override IEnumerable<Expression> MapInvocationExpression(InvocationExpression expression)
        {
            return Unwrap(expression, expression.Arguments, expression.Expression);
        }

        protected override IEnumerable<Expression> MapMethodCallExpression(MethodCallExpression expression)
        {
            return Unwrap(expression, expression.Object, expression.Arguments);
        }

        protected override IEnumerable<Expression> MapParameterExpression(ParameterExpression expression)
        {
            yield return expression;
        }

        protected override IEnumerable<Expression> MapUnaryExpression(UnaryExpression expression)
        {
            return Unwrap(expression, expression.Operand);
        }
        
        protected override IEnumerable<Expression> MapLambdaExpression(LambdaExpression expression)
        {
            yield return expression;
            foreach (var e in Map(expression.Body))
            {
                yield return e;
            }
        }

        protected override IEnumerable<Expression> MapMemberExpression(MemberExpression expression)
        {
            yield return expression;

            
        }

        protected override IEnumerable<Expression> MapNullExpression()
        {
            yield break;
        }

        protected override IEnumerable<Expression> MapExpression(Expression expression)
        {
            yield return expression;
        }

        private IEnumerable<Expression> Unwrap(Expression parent, params object[] children)
        {
            yield return parent;

            foreach (var e in children)
            {
                var expressionChild = e as Expression;
                if (expressionChild != null)
                {
                    foreach (var ee in Map(expressionChild))
                    {
                        yield return ee;
                    }

                    continue;
                }

                var expressionsChild = e as IEnumerable<Expression>;
                if (expressionsChild != null)
                {
                    foreach (var ec in expressionsChild)
                    {
                        foreach (var ee in Map(ec))
                        {
                            yield return ee;
                        }
                    }

                    continue;
                }

                throw new InvalidOperationException($"Unexpected parameter");
            }
        }
    }
}
