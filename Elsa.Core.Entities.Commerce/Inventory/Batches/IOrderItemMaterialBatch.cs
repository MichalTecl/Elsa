using System;


using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Batches
{
    [Entity]
    public interface IOrderItemMaterialBatch
    {
        long Id { get; }

        long OrderItemId { get; set; }
        IOrderItem OrderItem { get; }

        int MaterialBatchId { get; set; }
        IMaterialBatch MaterialBatch { get; }

        decimal Quantity { get; set; }

        int UserId { get; set; }
        IUser User { get; }

        DateTime AssignmentDt { get; set; }
    }
}
