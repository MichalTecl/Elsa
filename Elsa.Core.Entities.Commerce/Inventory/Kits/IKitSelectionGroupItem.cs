using Newtonsoft.Json;

using Robowire.RobOrm.Core;

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
    }
}
