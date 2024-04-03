using System;
using System.Text;

using Robowire.RobOrm.Core.Internal;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments.UnaryOperations
{
    public class CastOperation : IQuerySegment, IBooleanSegment
    {
        private readonly IQuerySegment m_operand;
        private readonly Type m_targetType;

        public CastOperation(Type targetType, IQuerySegment operand)
        {
            m_targetType = targetType;
            m_operand = operand;
        }

        public void Render(StringBuilder sb)
        {
            var sqlTypeName = SqlTypeMapper.GetSqlTypeName(m_targetType, 0);
            if (string.IsNullOrWhiteSpace(sqlTypeName))
            {
                m_operand.Render(sb);
                return;
            }

            sb.Append("CAST(");
            m_operand.Render(sb);
            sb.Append(" AS ");
            sb.Append(sqlTypeName);
            sb.Append(")");
        }

        public void RenderAsBoolean(StringBuilder sb)
        {
            sb.Append("(");
            Render(sb);
            sb.Append(" = 1)");
        }
    }
}
