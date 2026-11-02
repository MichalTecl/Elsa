using System.Text;

namespace RobOrm.Core.Query.Filtering.QuerySegments.Misc
{
    public class ColumnSegment : IQuerySegment, IBooleanSegment
    {
        private readonly string _tableAlias;
        private readonly string _column;

        public ColumnSegment(string tableAlias, string column)
        {
            _tableAlias = tableAlias;
            _column = column;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append("[");
            sb.Append(_tableAlias);
            sb.Append("].[");
            sb.Append(_column);
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
