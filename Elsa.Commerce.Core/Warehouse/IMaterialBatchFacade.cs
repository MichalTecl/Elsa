using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core.Warehouse
{
    public interface IMaterialBatchFacade
    {
        void AssignOrderItemToBatch(int batchId, IPurchaseOrder order, IOrderItem orderItem, decimal assignmentQuantity);

        Amount GetAvailableAmount(int batchId);

        IEnumerable<OrderItemBatchAssignmentModel> CreateBatchesAssignmentProposal(IPurchaseOrder order);
    }
}
