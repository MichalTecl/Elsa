using System;
using System.Linq.Expressions;

using RobOrm.Core.Expressions;
using RobOrm.Core.Query.Filtering;

namespace RobOrm.Core
{
    public interface IMethodMapper
    {
        IQuerySegment Map(MethodCallExpression expression, ExpressionMapperBase<IQuerySegment> queryMapper, Type resultingTableType, IHasParameters paramTarget);
    }
}
