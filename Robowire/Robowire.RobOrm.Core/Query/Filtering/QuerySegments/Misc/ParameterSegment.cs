using System.Linq.Expressions;
using System.Text;

using Robowire.Common.Expressions;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments.Misc
{
    internal class ParameterSegment : IQuerySegment, IBooleanSegment
    {
        private readonly IHasParameters m_target;
        private readonly string m_paramName;

        public ParameterSegment(Expression value, IHasParameters target)
        {
            m_target = target;
            m_paramName = $"@p{target.ParametersCount + 1}";
            var val = ExpressionEvaluator.Eval(value);
            m_target.AddParameter(m_paramName, val);
        }
        
        public void Render(StringBuilder sb)
        {
            sb.Append(m_paramName);
        }

        public void RenderAsBoolean(StringBuilder sb)
        {
            sb.Append("(");
            Render(sb);
            sb.Append(" = 1)");
        }
    }
}
