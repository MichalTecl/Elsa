using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

using Robowire.RobOrm.Core;

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
            string supplierName,
            string currencySymbol,
            string variableSymbol);

        IEnumerable<IMaterialStockEvent> GetBatchEvents(int materialBatchId);

        IEnumerable<IMaterialBatchComposition> GetCompositionsByComponentBatchId(int componentBatchId);

        void UpdateBatchAvailability(int batchId, bool isAvailable);

        MaterialBatchComponent CreateProductionBatch(int materialId, string batchNumber, decimal amount, IMaterialUnit unit, decimal productionWorkPrice);
        void MarkBatchAllProductionStepsDone(int batchId);

        string GetBatchNumberById(int batchId);

        int? GetBatchIdByNumber(int materialId, string batchNumber);

        IEnumerable<int> QueryBatchIds(Action<IQueryBuilder<IMaterialBatch>> customize);

        IEnumerable<IMaterialBatch> QueryBatches(Action<IQueryBuilder<IMaterialBatch>> customize);

        MaterialBatchComponent UpdateBatch(int id, Action<IMaterialBatchEditables> edit);

        IEnumerable<IBatchProductionStep> GetPerformedSteps(int batchId);

        IEnumerable<MaterialBatchComponent> GetBatchesByComponentInventory(int componentMaterialInventoryId, int compositionYear, int compositionMonth);
    }
}
