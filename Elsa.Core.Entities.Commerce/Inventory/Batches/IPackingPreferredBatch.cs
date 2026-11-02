using System;

using Elsa.Core.Entities.Commerce.Common.Security;

using RobOrm.Core;
using RobOrm.SqlServer.Attributes;

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

        [NVarchar(64, true)]
        string BatchNumber { get; set; }

        DateTime LastActivity { get; set; }
    }
}
