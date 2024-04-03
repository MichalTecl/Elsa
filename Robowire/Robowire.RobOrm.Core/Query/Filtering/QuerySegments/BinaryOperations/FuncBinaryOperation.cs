using System.Linq.Expressions;
using System.Text;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments.BinaryOperations
{
    public class FuncBinaryOperation : IQuerySegment, IBooleanSegment
    {
        private readonly string m_operation;
        private readonly IQuerySegment m_left;
        private readonly IQuerySegment m_right;

        private FuncBinaryOperation(string operation, IQuerySegment left, IQuerySegment right)
        {
            m_operation = operation;
            m_left = left;
            m_right = right;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append(m_operation).Append("(");
            m_left.Render(sb);
            sb.Append(", ");
            m_right.Render(sb);
            sb.Append(")");
        }

        public void RenderAsBoolean(StringBuilder sb)
        {
            sb.Append("(");
            Render(sb);
            sb.Append(" =1)");
        }

        public static IQuerySegment TryGet(ExpressionType type, IQuerySegment left, IQuerySegment right)
        {
            if (type == ExpressionType.Coalesce)
            {
                return new FuncBinaryOperation("ISNULL", left, right);
            }

            if (type == ExpressionType.Power)
            {
                return new FuncBinaryOperation("POWER", left, right);
            }

            return null;
        }
    }
}
