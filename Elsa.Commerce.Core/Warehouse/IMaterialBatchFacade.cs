using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Commerce.Core.Model.BatchPriceExpl;
using Elsa.Commerce.Core.Model.ProductionSteps;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IMaterialBatchFacade : IBatchKeyResolver
    {
        void AssignOrderItemToBatch(int batchId, IPurchaseOrder order, long orderItemId, decimal assignmentQuantity);

        Amount GetAvailableAmount(int batchId);

        void PreloadBatchAmountCache();

        IMaterialBatchStatus GetBatchStatus(int batchId);

        IEnumerable<OrderItemBatchAssignmentModel> TryResolveBatchAssignments(IPurchaseOrder order, Tuple<long, int, decimal> orderItemBatchPreference = null);

        IMaterialBatch FindBatchBySearchQuery(int materialId, string query);

        bool AlignOrderBatches(long purchaseOrderId);

        void ChangeOrderItemBatchAssignment(IPurchaseOrder order, long orderItemId, int batchId, decimal? requestNewAmount);

        void AssignComponent(int parentBatchId, int componentBatchId, Amount amountToAssign);

        void UnassignComponent(int parentBatchId, int componentBatchId);

        IEnumerable<Tuple<IMaterialBatch, Amount>> AutoResolve(int materialId, Amount requiredAmount, bool unresolvedAsNullBatch = false, int? batchId = null);

        void SetBatchLock(int batchId, bool lockValue, string note);

        void DeleteBatch(int batchId);
        void ReleaseBatchAmountCache(IMaterialBatch batch);

        IEnumerable<string> GetDeletionBlockReasons(int batchId);

        IEnumerable<MaterialLevelModel> GetMaterialLevels(bool includeUnwatched = false);

        MaterialLevelModel GetMaterialLevel(int materialId);
        
        int GetMaterialIdByBatchId(int batchId);

        BatchEventAmountSuggestions GetEventAmountSuggestions(int eventTypeId, int batchId);

        IEnumerable<IMaterialBatch> FindBatchesWithMissingInvoiceItem(int inventoryId);

        IEnumerable<IMaterialBatch> FindNotClosedBatches(int inventoryId, DateTime from, DateTime to, Func<IMaterialBatch, bool> filter = null);
        
        IEnumerable<BatchStepProgressInfo> GetProductionStepsProgress(IMaterialBatch batch);

        BatchAccountingDate GetBatchAccountingDate(IMaterialBatch batch);

        Tuple<decimal, BatchPrice> GetPriceOfAmount(int batchId, Amount amount);

        BatchPrice GetBatchPrice(int batchId);

        Amount GetNumberOfProducedProducts(int accountingDateYear, int accountingDateMonth, int inventoryId);
        void ReleaseUnsentOrdersAllocations();
        void CutOrderAllocation(int orderId, BatchKey key);

        IEnumerable<Tuple<int?, Amount>> ProposeAllocations(int materialId, string batchNumber, Amount requestedAmount);
    }
}
