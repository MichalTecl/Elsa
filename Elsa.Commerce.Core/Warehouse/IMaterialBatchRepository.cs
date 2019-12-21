using System;
using System.Collections.Generic;
using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IMaterialBatchRepository : IBatchKeyResolver
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

        IEnumerable<int> GetBatchIds(DateTime from,
            DateTime to,
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
            string supplierName,
            string currencySymbol,
            string variableSymbol);

        IEnumerable<IMaterialStockEvent> GetBatchEvents(int materialBatchId);

        IEnumerable<IMaterialBatchComposition> GetCompositionsByComponentBatchId(int componentBatchId);

        void UpdateBatchAvailability(int batchId, bool isAvailable);
    
        string GetBatchNumberById(int batchId);

        int? GetBatchIdByNumber(int materialId, string batchNumber);

        IEnumerable<IMaterialBatch> GetBatches(BatchKey key);

        IEnumerable<int> QueryBatchIds(Action<IQueryBuilder<IMaterialBatch>> customize);

        IEnumerable<IMaterialBatch> QueryBatches(Action<IQueryBuilder<IMaterialBatch>> customize);

        MaterialBatchComponent UpdateBatch(int id, Action<IMaterialBatchEditables> edit);
        
        IEnumerable<MaterialBatchComponent> GetBatchesByComponentInventory(int componentMaterialInventoryId, int compositionYear, int compositionMonth);

        IEnumerable<IMaterialBatchComposition> GetBatchComponents(int compositionId);

        IEnumerable<IMaterialBatch> GetBatchesByInvoiceNumber(string invoiceNumber, int supplierId);

        IMaterialBatch CreateBatchWithComponents(int recipeId, Amount amount, string batchNumber, decimal productionPrice, List<Tuple<BatchKey, Amount>> components, int? replaceBatchId);
    }
}
