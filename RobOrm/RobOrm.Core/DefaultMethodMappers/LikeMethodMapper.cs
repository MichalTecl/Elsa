using System;
using System.Linq.Expressions;
using System.Text;

using RobOrm.Core.Expressions;
using RobOrm.Core.Query.Filtering;
using RobOrm.Core.Query.Filtering.QuerySegments;

namespace RobOrm.Core.DefaultMethodMappers
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
            private readonly IQuerySegment _mappedLeft;
            private readonly IQuerySegment _mappedRight;

            public LikeSegment(IQuerySegment mappedLeft, IQuerySegment mappedRight)
            {
                _mappedLeft = mappedLeft;
                _mappedRight = mappedRight;
            }

            public void Render(StringBuilder sb)
            {
                sb.Append("(");
                _mappedLeft.Render(sb);
                sb.Append(" LIKE ");
                _mappedRight.Render(sb);
                sb.Append(")");
            }

            public void RenderAsBoolean(StringBuilder sb)
            {
                Render(sb);
            }
        }
    }

}
