using System.Linq.Expressions;
using System.Text;

using RobOrm.Core.Expressions;

namespace RobOrm.Core.Query.Filtering.QuerySegments.Misc
{
    internal class ParameterSegment : IQuerySegment, IBooleanSegment
    {
        private readonly IHasParameters _target;
        private readonly string _paramName;

        public ParameterSegment(Expression value, IHasParameters target)
        {
            _target = target;
            _paramName = $"@p{target.ParametersCount + 1}";
            var val = ExpressionEvaluator.Eval(value);
            _target.AddParameter(_paramName, val);
        }
        
        public void Render(StringBuilder sb)
        {
            sb.Append(_paramName);
        }

        public void RenderAsBoolean(StringBuilder sb)
        {
            sb.Append("(");
            Render(sb);
            sb.Append(" = 1)");
        }
    }
}
