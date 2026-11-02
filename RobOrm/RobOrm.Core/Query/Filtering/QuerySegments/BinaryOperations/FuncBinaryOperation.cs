using System.Linq.Expressions;
using System.Text;

namespace RobOrm.Core.Query.Filtering.QuerySegments.BinaryOperations
{
    public class FuncBinaryOperation : IQuerySegment, IBooleanSegment
    {
        private readonly string _operation;
        private readonly IQuerySegment _left;
        private readonly IQuerySegment _right;

        private FuncBinaryOperation(string operation, IQuerySegment left, IQuerySegment right)
        {
            _operation = operation;
            _left = left;
            _right = right;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append(_operation).Append("(");
            _left.Render(sb);
            sb.Append(", ");
            _right.Render(sb);
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
