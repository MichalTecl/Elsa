using System.Collections.Generic;
using Elsa.Commerce.Core.Model;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IBatchPriceBulkProvider
    {
        List<PriceComponentModel> GetBatchPriceComponents(int batchId);
    }
}
