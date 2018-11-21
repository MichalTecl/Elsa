using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IMaterialBatchFacade
    {
        void AssignOrderItemToBatch(int batchId, IPurchaseOrder order, long orderItemId, decimal assignmentQuantity);

        Amount GetAvailableAmount(int batchId);

        IEnumerable<OrderItemBatchAssignmentModel> TryResolveBatchAssignments(IPurchaseOrder order, Tuple<long, int, decimal> orderItemBatchPreference = null);

        IMaterialBatch FindBatchBySearchQuery(int materialId, string query);

        bool AlignOrderBatches(long purchaseOrderId);

        void ChangeOrderItemBatchAssignment(IPurchaseOrder order, long orderItemId, int batchId, decimal? requestNewAmount);

        void AssignComponent(int parentBatchId, int componentBatchId, Amount amountToAssign);

        void UnassignComponent(int parentBatchId, int componentBatchId);
    }
}
