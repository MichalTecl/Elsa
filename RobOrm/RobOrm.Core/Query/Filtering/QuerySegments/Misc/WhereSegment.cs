using System.Text;

namespace RobOrm.Core.Query.Filtering.QuerySegments.Misc
{
    public class WhereSegment : IQuerySegment
    {
        private readonly IQuerySegment _condition;

        public WhereSegment(IQuerySegment condition)
        {
            _condition = condition;
        }

        public void Render(StringBuilder sb)
        {
            sb.Append(" WHERE (");
            _condition.RenderBoolean(sb);
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
            _condition.Render(sb);
            return sb.ToString();
        }
    }
}
