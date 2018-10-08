using Newtonsoft.Json;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory.Kits
{
    [Entity]
    public interface IKitSelectionGroupItem : IMappedToOrderItem
    {
        int Id { get; }

        [JsonIgnore]
        int KitSelectionGroupId { get; set; }

        [JsonIgnore]
        IKitSelectionGroup KitSelectionGroup { get; }

        [NVarchar(64, true)]
        string Shortcut { get; set; }
    }
}
