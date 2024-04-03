using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

using Robowire.RobOrm.Core.Query.Filtering.QuerySegments.Misc;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments.BinaryOperations
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

        private readonly string m_symbol;
        private readonly IQuerySegment m_left;
        private readonly IQuerySegment m_right;

        private MathBinaryOperation(string symbol, IQuerySegment left, IQuerySegment right)
        {
            m_symbol = symbol;
            m_left = left;
            m_right = right;
        }

        public void Render(StringBuilder sb)
        {
            var nullRight = m_right as ConstantSegment;
            if ((nullRight != null) && nullRight.IsNull
                && ((m_symbol == s_supportedSymbols[ExpressionType.Equal])
                    || (m_symbol == s_supportedSymbols[ExpressionType.NotEqual])))
            {
                RenderNullCheck(sb, m_symbol == s_supportedSymbols[ExpressionType.Equal]);
                return;
            }

            sb.Append("(");
            m_left.Render(sb);
            sb.Append(m_symbol);
            m_right.Render(sb);
            sb.Append(")");
        }

        private void RenderNullCheck(StringBuilder sb, bool positive)
        {
            sb.Append("(");
            m_left.Render(sb);

            sb.Append(positive ? " IS NULL)" : " IS NOT NULL)");
        }

        internal static IQuerySegment TryGet(ExpressionType type, IQuerySegment left, IQuerySegment right)
        {
            string symbol;
            return !s_supportedSymbols.TryGetValue(type, out symbol) ? null : new MathBinaryOperation(symbol, left, right);
        }
    }
}
