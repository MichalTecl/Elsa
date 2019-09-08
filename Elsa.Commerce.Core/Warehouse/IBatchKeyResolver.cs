using System;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IBatchKeyResolver
    {
        Tuple<int, string> GetBatchNumberAndMaterialIdByBatchId(int batchId);
    }
}
