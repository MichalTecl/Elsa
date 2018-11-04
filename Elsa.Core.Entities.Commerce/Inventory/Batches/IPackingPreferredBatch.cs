using System;

using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Batches
{
    [Entity]
    public interface IPackingPreferredBatch
    {
        int Id { get; }

        int UserId { get; set; }
        IUser User { get; }

        int MaterialId { get; set; }
        IMaterial Material { get; }

        int BatchId { get; set; }
        IMaterialBatch Batch { get; }

        DateTime LastActivity { get; set; }
    }
}
