using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using Robowire.Common.Expressions;
using Robowire.RobOrm.Core.Query.Abstraction;
using Robowire.RobOrm.Core.Query.Filtering;

namespace Robowire.RobOrm.Core
{
    public interface IQueryBuilder<T> : IHasParameters where T : class
    {
        IQueryBuilder<T> Join<T2>(Expression<Func<T, T2>> expression);

        IQueryBuilder<T> Where(Expression<Func<T, bool>> condition);

        IQueryBuilder<T> OrderBy(Expression<Func<T, object>> expression);

        IQueryBuilder<T> OrderByDesc(Expression<Func<T, object>> expression); 
        
        ITransformedQuery<TResult> Transform<TResult>(Expression<Func<T, TResult>> generator);

        //void Transform<TResult>(Expression<Action<TResult, T>> mapper);

        IQueryBuilder<T> Take(int rowsCount);

        IQueryBuilder<T> Skip(int rowsCount);

        IQueryModel<T> Build();

        IEnumerable<T> Execute();

        IQuerySegment ToSegment(ExpressionMapperBase<IQuerySegment> queryMapper, IHasParameters paramsTarget);
    }
}
