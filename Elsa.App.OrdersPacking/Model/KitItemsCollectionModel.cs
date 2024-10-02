using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Kits;

using Newtonsoft.Json;

namespace Elsa.App.OrdersPacking.Model
{
    public class KitItemsCollectionModel : KitItemsCollection
    {
        public KitItemsCollectionModel(KitItemsCollection source)
            : base(source.KitDefinitionId, source.GroupItems, source.SelectedItem, source.KitItemIndex, source.GroupId, source.GroupName)
        {
            SelectedItem = source.SelectedItem;
        }

        [JsonIgnore]
        public override IOrderItem SelectedItem { get; }

        [JsonProperty("SelectedItem")]
        public PackingOrderItemModel SelectedItemModel { get; set; }
    }
}
