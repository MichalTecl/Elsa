using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IMaterialBatchRepository
    {
        MaterialBatchComponent GetBatchById(int id);

        MaterialBatchComponent GetBatchByNumber(int materialId, string batchNumber);
        
        IEnumerable<MaterialBatchComponent> GetMaterialBatches(
            DateTime from,
            DateTime to,
            bool excludeCompositions,
            int? materialId,
            bool includeLocked = false,
            bool includeClosed = false,
            bool includedUnavailable = false);

        MaterialBatchComponent SaveBottomLevelMaterialBatch(
            int id,
            IMaterial material,
            decimal amount,
            IMaterialUnit unit,
            string batchNr,
            DateTime receiveDt,
            decimal price,
            string invoiceNr,
            string supplierName);

        IEnumerable<IMaterialStockEvent> GetBatchEvents(int materialBatchId);

        IEnumerable<IMaterialBatchComposition> GetCompositionsByComponentBatchId(int componentBatchId);

        void UpdateBatchAvailability(int batchId, bool isAvailable);

        MaterialBatchComponent CreateProductionBatch(int materialId, string batchNumber, decimal amount, IMaterialUnit unit);
        void MarkBatchAllProductionStepsDone(int batchId);

        string GetBatchNumberById(int batchId);

        int? GetBatchIdByNumber(int materialId, string batchNumber);
    }
}
