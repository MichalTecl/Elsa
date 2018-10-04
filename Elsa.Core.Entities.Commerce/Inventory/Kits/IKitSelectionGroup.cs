using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Kits
{
    [Entity]
    public interface IKitSelectionGroup
    {
        int Id { get; }

        int KitDefinitionId { get; set; }

        IKitDefinition KitDefinition { get; }

        IEnumerable<IKitSelectionGroupItem> Items { get; }
    }
}
