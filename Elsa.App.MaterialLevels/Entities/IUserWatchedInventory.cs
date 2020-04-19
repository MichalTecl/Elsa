using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire.RobOrm.Core;

namespace Elsa.App.MaterialLevels.Entities
{
    [Entity]
    public interface IUserWatchedInventory : IIntIdEntity
    {
        int InventoryId { get; set; }
        IMaterialInventory Inventory { get; }

        int UserId { get; set; }
        IUser User { get; }
    }
}
