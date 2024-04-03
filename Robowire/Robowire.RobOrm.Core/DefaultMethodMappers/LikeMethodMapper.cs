using System;
using System.Linq.Expressions;
using System.Text;

using Robowire.Common.Expressions;
using Robowire.RobOrm.Core.Query.Filtering;
using Robowire.RobOrm.Core.Query.Filtering.QuerySegments;

namespace Robowire.RobOrm.Core.DefaultMethodMappers
{
    public class LikeMethodMapper : IMethodMapper
    {
        public IQuerySegment Map(MethodCallExpression expression, ExpressionMapperBase<IQuerySegment> queryMapper, Type resultingTableType, IHasParameters p)
        {
            if (expression.Arguments.Count != 2)
            {
                throw new InvalidOperationException("Two arguments required");
            }

            var l = queryMapper.Map(expression.Arguments[0]);
            var r = queryMapper.Map(expression.Arguments[1]);

            return new LikeSegment(l, r);
        }

        private class LikeSegment : IQuerySegment, IBooleanSegment
        {
            private readonly IQuerySegment m_mappedLeft;
            private readonly IQuerySegment m_mappedRight;

            public LikeSegment(IQuerySegment mappedLeft, IQuerySegment mappedRight)
            {
                m_mappedLeft = mappedLeft;
                m_mappedRight = mappedRight;
            }

            public void Render(StringBuilder sb)
            {
                sb.Append("(");
                m_mappedLeft.Render(sb);
                sb.Append(" LIKE ");
                m_mappedRight.Render(sb);
                sb.Append(")");
            }

            public void RenderAsBoolean(StringBuilder sb)
            {
                Render(sb);
            }
        }
    }

}
