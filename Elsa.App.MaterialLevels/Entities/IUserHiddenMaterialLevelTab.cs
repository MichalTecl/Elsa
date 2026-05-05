using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.MaterialLevels.Entities
{
    [Entity]
    public interface IUserHiddenMaterialLevelTab : IIntIdEntity
    {
        int InventoryId { get; set; }
        IMaterialInventory Inventory { get; }

        [NVarchar(256, true)]
        string MaterialLevelReportingGroup { get; set; }

        int UserId { get; set; }
        IUser User { get; }
    }
}
