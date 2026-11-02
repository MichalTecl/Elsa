using System.Collections.Generic;

using RobOrm.Core.Expressions;
using RobOrm.Core.Query.Filtering;

namespace RobOrm.Core
{
    public interface ITransformedQuery<T> : ITransformedQuery
    {
        IEnumerable<T> Execute();
    }

    public interface ITransformedQuery
    {
        IQuerySegment GetQuery(ExpressionMapperBase<IQuerySegment> queryMapper, IHasParameters paramsTarget);
    }
}
