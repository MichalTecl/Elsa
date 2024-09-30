using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

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

        [NVarchar(126, true)]
        string Name { get; set; }

        [NVarchar(256, true)]
        string InTextMarker { get; set; }

        IEnumerable<IKitSelectionGroupItem> Items { get; }
    }
}
