using System;
using System.Collections;
using System.Linq.Expressions;
using System.Text;

using Robowire.Common.Expressions;
using Robowire.RobOrm.Core.Query.Filtering;
using Robowire.RobOrm.Core.Query.Filtering.QuerySegments;

namespace Robowire.RobOrm.Core.DefaultMethodMappers
{
    public class InCsvMethodMapper : IMethodMapper
    {
        public IQuerySegment Map(MethodCallExpression expression, ExpressionMapperBase<IQuerySegment> queryMapper, Type resultingTableType, IHasParameters paramTarget)
        {
            var subject = queryMapper.Map(expression.Arguments[0]);
            var itemsLst = ExpressionEvaluator.Eval(expression.Arguments[1]) as IEnumerable;
            return new InCsvSegment(subject, itemsLst);
        }

        private class InCsvSegment : IQuerySegment, IBooleanSegment
        {
            private readonly IQuerySegment m_subject;
            private readonly IEnumerable m_items;

            public InCsvSegment(IQuerySegment subject, IEnumerable items)
            {
                m_subject = subject;
                m_items = items;
            }

            public void Render(StringBuilder sb)
            {
                sb.Append("(");
                m_subject.Render(sb);
                sb.Append(" IN (");

                var first = true;
                foreach (var i in m_items)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    first = false;

                    if (i is string)
                    {
                        sb.Append($"'{i}'");
                    }
                    else
                    {
                        sb.Append(i);
                    }
                }

                sb.Append("))");
            }

            public void RenderAsBoolean(StringBuilder sb)
            {
                Render(sb);
            }
        }
    }
}
