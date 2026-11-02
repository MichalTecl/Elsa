using System;
using System.Text;

using RobOrm.Core.Internal;

namespace RobOrm.Core.Query.Filtering.QuerySegments.UnaryOperations
{
    public class CastOperation : IQuerySegment, IBooleanSegment
    {
        private readonly IQuerySegment _operand;
        private readonly Type _targetType;

        public CastOperation(Type targetType, IQuerySegment operand)
        {
            _targetType = targetType;
            _operand = operand;
        }

        public void Render(StringBuilder sb)
        {
            var sqlTypeName = SqlTypeMapper.GetSqlTypeName(_targetType, 0);
            if (string.IsNullOrWhiteSpace(sqlTypeName))
            {
                _operand.Render(sb);
                return;
            }

            sb.Append("CAST(");
            _operand.Render(sb);
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
