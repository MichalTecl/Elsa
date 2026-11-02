using System.Linq.Expressions;
using System.Text;

namespace RobOrm.Core.Query.Filtering.QuerySegments.BinaryOperations
{
    internal class LogicalBinaryOperation : IQuerySegment, IBooleanSegment
    {
        private readonly string _symbol;
        private readonly IQuerySegment _left;
        private readonly IQuerySegment _right;

        private LogicalBinaryOperation(string symbol, IQuerySegment left, IQuerySegment right)
        {
            _symbol = symbol;
            _left = left;
            _right = right;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append("(");
            _left.RenderBoolean(sb);
            sb.Append(_symbol);
            _right.RenderBoolean(sb);
            sb.Append(")");
        }

        public void RenderAsBoolean(StringBuilder sb)
        {
            Render(sb);
        }

        public static IQuerySegment TryGet(ExpressionType type, IQuerySegment left, IQuerySegment right)
        {
            if (type == ExpressionType.AndAlso)
            {
                return new LogicalBinaryOperation(" AND ", left, right);
            }

            if (type == ExpressionType.OrElse)
            {
                return new LogicalBinaryOperation(" OR ", left, right);
            }

            return null;
        }
    }
}
