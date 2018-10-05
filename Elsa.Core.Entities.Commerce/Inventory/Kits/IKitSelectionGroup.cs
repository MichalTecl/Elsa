using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Kits
{
    [Entity]
    public interface IKitSelectionGroup
    {
        int Id { get; }

        [JsonIgnore]
        int KitDefinitionId { get; set; }

        [JsonIgnore]
        IKitDefinition KitDefinition { get; }

        IEnumerable<IKitSelectionGroupItem> Items { get; }
    }
}
