using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Kits
{
    [Entity]
    public interface IKitSelectionGroupItem : IMappedToOrderItem
    {
        int Id { get; }

        int KitSelectionGroupId { get; set; }

        IKitSelectionGroup KitSelectionGroup { get; }
    }
}
