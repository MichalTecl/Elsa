using System;
using System.Linq.Expressions;

using Robowire.Common.Expressions;
using Robowire.RobOrm.Core.Query.Filtering;

namespace Robowire.RobOrm.Core
{
    public interface IMethodMapper
    {
        IQuerySegment Map(MethodCallExpression expression, ExpressionMapperBase<IQuerySegment> queryMapper, Type resultingTableType, IHasParameters paramTarget);
    }
}
