using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Kits;

namespace Elsa.Commerce.Core.Model
{
    public class KitItemsCollection
    {
        public KitItemsCollection(IEnumerable<IKitSelectionGroupItem> groupItems, IOrderItem selectedItem, int kitItemIndex, int groupId, string groupName)
        {
            GroupItems = groupItems;
            SelectedItem = selectedItem;
            KitItemIndex = kitItemIndex;
            GroupId = groupId;
            GroupName = groupName;
        }

        public int GroupId { get; }

        public int KitItemIndex { get; }

        public IEnumerable<IKitSelectionGroupItem> GroupItems { get; }

        public IOrderItem SelectedItem { get; }

        public string GroupName { get; }
    }
}
