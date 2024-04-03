using System.Text;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments.UnaryOperations
{
    public class NotOperation : IQuerySegment
    {
        private readonly IQuerySegment m_operand;

        public NotOperation(IQuerySegment operand)
        {
            m_operand = operand;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append("NOT(");
            m_operand.RenderBoolean(sb);
            sb.Append(")");
        }
    }
}
