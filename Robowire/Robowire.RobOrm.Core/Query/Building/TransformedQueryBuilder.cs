using System.Collections.Generic;

using Robowire.Common.Expressions;
using Robowire.RobOrm.Core.Query.Filtering;

namespace Robowire.RobOrm.Core.Query.Building
{
    internal class TransformedQueryBuilder<TSource, TTarget> : ITransformedQuery<TTarget> where TSource : class
    {
        private readonly IDatabase m_database;
        private readonly IQueryBuilder<TSource> m_sourceBuilder;

        public TransformedQueryBuilder(IDatabase database, IQueryBuilder<TSource> sourceBuilder)
        {
            m_database = database;
            m_sourceBuilder = sourceBuilder;
        }

        public IEnumerable<TTarget> Execute()
        {
            var model = m_sourceBuilder.Build();
            return m_database.SelectSingleColumn<TSource, TTarget>(model);
        }

        public IQuerySegment GetQuery(ExpressionMapperBase<IQuerySegment> queryMapper, IHasParameters paramsTarget)
        {
            return m_sourceBuilder.ToSegment(queryMapper, paramsTarget);
        }
    }
}
