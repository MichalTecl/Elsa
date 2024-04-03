using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Robowire.Common.Expressions;

namespace Robowire.RobOrm.Core.Internal
{
    public static class ExpressionsHelper
    {
        public static readonly MethodInfo EachMethod =
            typeof(CollectionExtensions).GetMethod(nameof(CollectionExtensions.Each));

        public static MemberExpression FindFirstMemberExpression(LambdaExpression expr)
        {
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    var ue = expr.Body as UnaryExpression;
                    me = ue?.Operand as MemberExpression;
                    break;
                default:
                    me = expr.Body as MemberExpression;
                    break;
            }

            return me;
        }

        public static string GetPropertiesChainText<T>(Expression expr)
        {
            var visitor = new PropertyChainVisitior<T>();
            return visitor.Map(expr);
        }

        public static bool IsEachMethod(MethodInfo method)
        {
            return (method.DeclaringType == EachMethod.DeclaringType) && (method.Name == EachMethod.Name);
        }

        public static Expression<Func<T, bool>> CombineConditions<T>(Expression<Func<T, bool>> filter1, Expression<Func<T, bool>> filter2)
        {
            var rewrittenBody1 = new ReplaceVisitor(filter1.Parameters[0], filter2.Parameters[0]).Visit(filter1.Body);
            var newFilter = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(rewrittenBody1, filter2.Body), filter2.Parameters);
            return newFilter;
        }

        private class PropertyChainVisitior<T> : ExpressionMapperBase<string>
        { 
            protected override string MapNullExpression()
            {
                throw new NotImplementedException();
            }

            protected override string MapExpression(Expression expression)
            {
                if (expression.NodeType == ExpressionType.Convert)
                {
                    return Map(((UnaryExpression)expression).Operand);
                }

                return null;
            }

            protected override string MapMethodCallExpression(MethodCallExpression expression)
            {
                if (IsEachMethod(expression.Method))
                {
                    return Map(expression.Arguments.Single());
                }

                return null;
            }

            protected override string MapParameterExpression(ParameterExpression expression)
            {
                if (expression.Type != typeof(T))
                {
                    throw new InvalidOperationException($"Unexpected parameter type {expression.Type}");
                }

                return typeof(T).Name;
            }

            protected override string MapMemberExpression(MemberExpression expression)
            {
                var parent = Map(expression.Expression);
                if (parent == null)
                {
                    return null;
                }
                
                return DotPathHelper.Combine(parent, expression.Member.Name);
            }
        }

        private class ReplaceVisitor : ExpressionVisitor
        {
            private readonly Expression m_from, m_to;
            public ReplaceVisitor(Expression from, Expression to)
            {
                m_from = from;
                m_to = to;
            }
            public override Expression Visit(Expression node)
            {
                return node == m_from ? m_to : base.Visit(node);
            }
        }

        public static Expression RemoveConvert(Expression orderExpression)
        {
            var lambda = orderExpression as LambdaExpression;
            if (lambda != null)
            {
                return RemoveConvert(lambda.Body);
            }

            var conversion = orderExpression as UnaryExpression;
            if (conversion != null)
            {
                return RemoveConvert(conversion.Operand);
            }

            return orderExpression;
        }

    }
}
