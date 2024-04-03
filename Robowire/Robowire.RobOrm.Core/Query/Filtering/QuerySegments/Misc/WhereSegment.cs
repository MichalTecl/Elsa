using System.Text;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments.Misc
{
    public class WhereSegment : IQuerySegment
    {
        private readonly IQuerySegment m_condition;

        public WhereSegment(IQuerySegment condition)
        {
            m_condition = condition;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append(" WHERE (");
            m_condition.RenderBoolean(sb);
            sb.Append(")");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            Render(sb);

            return sb.ToString();
        }

        public string GetSelectExpression()
        {
            var sb = new StringBuilder();
            m_condition.Render(sb);
            return sb.ToString();
        }
    }
}
