using System.Text;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments.Misc
{
    public class ColumnSegment : IQuerySegment, IBooleanSegment
    {
        private readonly string m_tableAlias;
        private readonly string m_column;

        public ColumnSegment(string tableAlias, string column)
        {
            m_tableAlias = tableAlias;
            m_column = column;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append("[");
            sb.Append(m_tableAlias);
            sb.Append("].[");
            sb.Append(m_column);
            sb.Append("]");
        }

        public void RenderAsBoolean(StringBuilder sb)
        {
            sb.Append("(");
            Render(sb);
            sb.Append(" = 1)");
        }
    }
}
