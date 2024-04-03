using System.Linq.Expressions;
using System.Text;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments.BinaryOperations
{
    internal class LogicalBinaryOperation : IQuerySegment, IBooleanSegment
    {
        private readonly string m_symbol;
        private readonly IQuerySegment m_left;
        private readonly IQuerySegment m_right;

        private LogicalBinaryOperation(string symbol, IQuerySegment left, IQuerySegment right)
        {
            m_symbol = symbol;
            m_left = left;
            m_right = right;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append("(");
            m_left.RenderBoolean(sb);
            sb.Append(m_symbol);
            m_right.RenderBoolean(sb);
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
