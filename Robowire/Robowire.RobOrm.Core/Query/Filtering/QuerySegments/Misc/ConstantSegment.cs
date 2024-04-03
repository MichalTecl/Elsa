using System.Linq.Expressions;
using System.Text;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments.Misc
{
    public class ConstantSegment : IQuerySegment, IBooleanSegment
    {
        private readonly ConstantExpression m_expression;
        
        public ConstantSegment(ConstantExpression expression)
        {
            m_expression = expression;
        }

        public bool IsNull
        {
            get
            {
                return m_expression.Value == null;
            }
        }

        public void Render(StringBuilder sb)
        {
            if (m_expression.Type == typeof(bool))
            {
                sb.Append((bool)m_expression.Value ? "1" : "0");
            }
            else if (m_expression.Type == typeof(string))
            {
                sb.Append("'").Append(m_expression.Value.ToString().Replace("'", "''")).Append("'");
            }
            else
            {
                sb.Append(m_expression.Value ?? "NULL");
            }
        }

        public void RenderAsBoolean(StringBuilder sb)
        {
            sb.Append("(");
            Render(sb);
            sb.Append(" = 1)");
        }
    }
}
