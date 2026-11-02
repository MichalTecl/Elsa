using System;
using System.Collections;
using System.Linq.Expressions;
using System.Text;

using RobOrm.Core.Expressions;
using RobOrm.Core.Query.Filtering;
using RobOrm.Core.Query.Filtering.QuerySegments;

namespace RobOrm.Core.DefaultMethodMappers
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
            private readonly IQuerySegment _subject;
            private readonly IEnumerable _items;

            public InCsvSegment(IQuerySegment subject, IEnumerable items)
            {
                _subject = subject;
                _items = items;
            }

            public void Render(StringBuilder sb)
            {
                sb.Append("(");
                _subject.Render(sb);
                sb.Append(" IN (");

                var first = true;
                foreach (var i in _items)
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
