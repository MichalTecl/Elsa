using System;
using System.Linq.Expressions;

using Robowire.Common.Expressions;
using Robowire.RobOrm.Core.Internal;
using Robowire.RobOrm.Core.Query.Filtering.QuerySegments.BinaryOperations;
using Robowire.RobOrm.Core.Query.Filtering.QuerySegments.Misc;
using Robowire.RobOrm.Core.Query.Filtering.QuerySegments.UnaryOperations;

namespace Robowire.RobOrm.Core.Query.Filtering
{
    public sealed class ExpressionQueryBuilder<T> : ExpressionMapperBase<IQuerySegment> where T : class
    {
        private readonly IQueryBuilder<T> m_owner;
        private WhereSegment m_whereSegment = null;

        public ExpressionQueryBuilder(IQueryBuilder<T> owner)
        {
            m_owner = owner;
        }

        public override IQuerySegment Map(Expression expression)
        {
            if (!ExpressionEvaluator.IsSelfEvaluable(expression) || (expression.NodeType == ExpressionType.Lambda))
            {
                return base.Map(expression);
            }

            var constant = expression as ConstantExpression;
            if (constant != null)
            {
                return new ConstantSegment(constant);
            }

            return new ParameterSegment(expression, m_owner);
        }

        protected override IQuerySegment MapLambdaExpression(LambdaExpression expression)
        {
            if (m_whereSegment != null)
            {
                return base.MapLambdaExpression(expression);
            }

            var condition = Map(expression.Body);

            m_whereSegment = new WhereSegment(condition);

            return m_whereSegment;
        }

        protected override IQuerySegment MapNullExpression()
        {
            throw new NotImplementedException();
        }

        protected override IQuerySegment MapExpression(Expression expression)
        {
            throw new NotSupportedException($"Expression {expression} is not supported");
        }

        protected override IQuerySegment MapBinaryExpression(BinaryExpression expression)
        {
            var mappedLeft = Map(expression.Left);
            var mappedRight = Map(expression.Right);

            var result = MathBinaryOperation.TryGet(expression.NodeType, mappedLeft, mappedRight) 
                      ?? LogicalBinaryOperation.TryGet(expression.NodeType, mappedLeft, mappedRight)
                      ?? FuncBinaryOperation.TryGet(expression.NodeType, mappedLeft, mappedRight);

            if (result == null)
            {
                throw new NotSupportedException($"Unsupported operation {expression}");
            }

            return result;
        }

        protected override IQuerySegment MapUnaryExpression(UnaryExpression expression)
        {
            var operand = Map(expression.Operand);

            switch (expression.NodeType)
            {
                case ExpressionType.Not:
                    return new NotOperation(operand);
                case ExpressionType.Convert:
                    return new CastOperation(expression.Type, operand);
                default:
                    throw new NotSupportedException($"Unsupported operation {expression}");
            }
        }

        protected override IQuerySegment MapParameterExpression(ParameterExpression expression)
        {
            throw new InvalidOperationException("I just didn't get it :(");
        }

        protected override IQuerySegment MapMemberExpression(MemberExpression expression)
        {
            var propChain = ExpressionsHelper.GetPropertiesChainText<T>(expression);
            if (string.IsNullOrWhiteSpace(propChain))
            {
                throw new NotSupportedException($"Unsupported expression {expression}");
            }

            var tableAndColumn = DotPathHelper.SplitToEntityAndAttribute(propChain);

            var tableAlias = tableAndColumn.Item1;
            var column = tableAndColumn.Item2;
            
            return new ColumnSegment(tableAlias, column);
        }

        protected override IQuerySegment MapMethodCallExpression(MethodCallExpression expression)
        {
            var mapper = MethodMapperAttribute.GetMapper(expression.Method);
            if (mapper == null)
            {
                throw new InvalidOperationException($"Method {expression.Method} used in {expression} must be marked by MethodMapperAttribute to be used in a query");
            }

            return mapper.Map(expression, this, typeof(T), m_owner);
        }

        protected override IQuerySegment MapConstantExpression(ConstantExpression expression)
        {
            throw new InvalidOperationException("Shuldn't happen, right?");
        }
    }
}
