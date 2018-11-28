using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Production.Model;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.Production
{
    public interface IProductionFacade
    {
        ProductionBatchModel GetProductionBatch(int batchId);

        ProductionBatchModel CreateOrUpdateProductionBatch(
            int? batchId,
            int materialId,
            string batchNumber,
            decimal amount,
            IMaterialUnit unit);

        ProductionBatchModel SetComponentSourceBatch(
            int? materialBatchCompositionId,
            int productionBatchId,
            int sourceBatchId,
            decimal usedAmount,
            string usedAmountUnitSymbol);

        ProductionBatchModel RemoveComponentSourceBatch(int productionBatchId, int sourceBatchId);

        IEnumerable<IMaterialBatch> LoadProductionBatches(long? fromDt, int pageSize);
    }
}
