using System.Collections.Generic;

using RobOrm.Core.Expressions;
using RobOrm.Core.Query.Filtering;

namespace RobOrm.Core.Query.Building
{
    internal class TransformedQueryBuilder<TSource, TTarget> : ITransformedQuery<TTarget> where TSource : class
    {
        private readonly IDatabase _database;
        private readonly IQueryBuilder<TSource> _sourceBuilder;

        public TransformedQueryBuilder(IDatabase database, IQueryBuilder<TSource> sourceBuilder)
        {
            _database = database;
            _sourceBuilder = sourceBuilder;
        }

        public IEnumerable<TTarget> Execute()
        {
            var model = _sourceBuilder.Build();
            return _database.SelectSingleColumn<TSource, TTarget>(model);
        }

        public IQuerySegment GetQuery(ExpressionMapperBase<IQuerySegment> queryMapper, IHasParameters paramsTarget)
        {
            return _sourceBuilder.ToSegment(queryMapper, paramsTarget);
        }
    }
}
