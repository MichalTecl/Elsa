using System;
using System.Linq.Expressions;
using System.Text;

using Robowire.Common.Expressions;
using Robowire.RobOrm.Core.Query.Filtering;
using Robowire.RobOrm.Core.Query.Filtering.QuerySegments;

namespace Robowire.RobOrm.Core.DefaultMethodMappers
{
    public class InSubqueryMethodMapper : IMethodMapper
    {
        protected virtual string Operator => " IN ";

        public IQuerySegment Map(MethodCallExpression expression, ExpressionMapperBase<IQuerySegment> queryMapper, Type resultingTableType, IHasParameters paramTarget)
        {
            var operand = queryMapper.Map(expression.Arguments[0]);

            var builtSubquery = ExpressionEvaluator.Eval(expression.Arguments[1]) as ITransformedQuery;

            return new InSubquerySegment(operand, builtSubquery.GetQuery(queryMapper, paramTarget), Operator);
        }

        private class InSubquerySegment : IQuerySegment, IBooleanSegment
        {
            private readonly IQuerySegment _operand;
            private readonly IQuerySegment _itemsSegment;
            private readonly string _operator;

            public InSubquerySegment(IQuerySegment operand, IQuerySegment itemsSegment, string @operator)
            {
                _operand = operand;
                _itemsSegment = itemsSegment;
                _operator = @operator;
            }

            public void Render(StringBuilder sb)
            {
                sb.Append("(");
                _operand.Render(sb);
                sb.Append(_operator);
                _itemsSegment.Render(sb);
                sb.Append(")");
            }

            public void RenderAsBoolean(StringBuilder sb)
            {
                Render(sb);
            }
        }
    }
}
