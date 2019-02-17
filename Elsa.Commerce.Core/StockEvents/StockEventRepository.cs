using System.Collections.Generic;

using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.StockEvents
{
    public class StockEventRepository : IStockEventRepository
    {
        private readonly IPerProjectDbCache m_cache;

        public StockEventRepository(IPerProjectDbCache cache)
        {
            m_cache = cache;
        }

        public IEnumerable<IStockEventType> GetAllEventTypes()
        {
            return m_cache.ReadThrough("stockEventTypes", q => q.SelectFrom<IStockEventType>().OrderBy(t => t.Name));
        }
    }
}
