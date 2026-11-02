using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using RobOrm.Core.Query.Filtering.QuerySegments.Misc;

namespace RobOrm.Core.Query.Filtering.QuerySegments.BinaryOperations
{
    internal class MathBinaryOperation : IQuerySegment
    {
        private static readonly Dictionary<ExpressionType, string> s_supportedSymbols = new Dictionary<ExpressionType, string>() {
            { ExpressionType.Add, "+" },
            {ExpressionType.AddChecked, "+"},
            {ExpressionType.Divide, "/"},
            {ExpressionType.Modulo, "%"},
            {ExpressionType.Multiply, "*"},
            {ExpressionType.MultiplyChecked, "*"},
            {ExpressionType.Subtract, "-"},
            {ExpressionType.SubtractChecked, "-"},
            {ExpressionType.And, "&"},
            {ExpressionType.Or, "|"},
            {ExpressionType.ExclusiveOr, "^"},
            {ExpressionType.Equal, "="},
            {ExpressionType.NotEqual, "<>"},
            {ExpressionType.GreaterThanOrEqual, ">="},
            {ExpressionType.GreaterThan, ">"},
            {ExpressionType.LessThan, "<"},
            {ExpressionType.LessThanOrEqual, "<="}};

        private readonly string _symbol;
        private readonly IQuerySegment _left;
        private readonly IQuerySegment _right;

        private MathBinaryOperation(string symbol, IQuerySegment left, IQuerySegment right)
        {
            _symbol = symbol;
            _left = left;
            _right = right;
        }

        public void Render(StringBuilder sb)
        {
            var nullRight = _right as ConstantSegment;
            if ((nullRight != null) && nullRight.IsNull
                && ((_symbol == s_supportedSymbols[ExpressionType.Equal])
                    || (_symbol == s_supportedSymbols[ExpressionType.NotEqual])))
            {
                RenderNullCheck(sb, _symbol == s_supportedSymbols[ExpressionType.Equal]);
                return;
            }

            sb.Append("(");
            _left.Render(sb);
            sb.Append(_symbol);
            _right.Render(sb);
            sb.Append(")");
        }

        private void RenderNullCheck(StringBuilder sb, bool positive)
        {
            sb.Append("(");
            _left.Render(sb);

            sb.Append(positive ? " IS NULL)" : " IS NOT NULL)");
        }

        internal static IQuerySegment TryGet(ExpressionType type, IQuerySegment left, IQuerySegment right)
        {
            string symbol;
            return !s_supportedSymbols.TryGetValue(type, out symbol) ? null : new MathBinaryOperation(symbol, left, right);
        }
    }
}
