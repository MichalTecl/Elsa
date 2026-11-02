using System.Linq.Expressions;
using System.Text;

namespace RobOrm.Core.Query.Filtering.QuerySegments.Misc
{
    public class ConstantSegment : IQuerySegment, IBooleanSegment
    {
        private readonly ConstantExpression _expression;
        
        public ConstantSegment(ConstantExpression expression)
        {
            _expression = expression;
        }

        public bool IsNull
        {
            get
            {
                return _expression.Value == null;
            }
        }

        public void Render(StringBuilder sb)
        {
            if (_expression.Type == typeof(bool))
            {
                sb.Append((bool)_expression.Value ? "1" : "0");
            }
            else if (_expression.Type == typeof(string))
            {
                sb.Append("'").Append(_expression.Value.ToString().Replace("'", "''")).Append("'");
            }
            else
            {
                sb.Append(_expression.Value ?? "NULL");
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
