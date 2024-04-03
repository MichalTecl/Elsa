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
        public IQuerySegment Map(MethodCallExpression expression, ExpressionMapperBase<IQuerySegment> queryMapper, Type resultingTableType, IHasParameters paramTarget)
        {
            var operand = queryMapper.Map(expression.Arguments[0]);

            var builtSubquery = ExpressionEvaluator.Eval(expression.Arguments[1]) as ITransformedQuery;

            return new InSubquerySegment(operand, builtSubquery.GetQuery(queryMapper, paramTarget));
        }

        private class InSubquerySegment : IQuerySegment, IBooleanSegment
        {
            private IQuerySegment m_operand;
            private IQuerySegment m_itemsSegment;

            public InSubquerySegment(IQuerySegment operand, IQuerySegment itemsSegment)
            {
                m_operand = operand;
                m_itemsSegment = itemsSegment;
            }

            public void Render(StringBuilder sb)
            {
                sb.Append("(");
                m_operand.Render(sb);
                sb.Append(" IN ");
                m_itemsSegment.Render(sb);
                sb.Append(")");
            }

            public void RenderAsBoolean(StringBuilder sb)
            {
                Render(sb);
            }
        }
    }
}
