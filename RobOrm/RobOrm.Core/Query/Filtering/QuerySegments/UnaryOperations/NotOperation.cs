using System.Text;

namespace RobOrm.Core.Query.Filtering.QuerySegments.UnaryOperations
{
    public class NotOperation : IQuerySegment
    {
        private readonly IQuerySegment _operand;

        public NotOperation(IQuerySegment operand)
        {
            _operand = operand;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append("NOT(");
            _operand.RenderBoolean(sb);
            sb.Append(")");
        }
    }
}
