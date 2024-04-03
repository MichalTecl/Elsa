using System.Collections.Generic;

using Robowire.Common.Expressions;
using Robowire.RobOrm.Core.Query.Filtering;

namespace Robowire.RobOrm.Core
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
