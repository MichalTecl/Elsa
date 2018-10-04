using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory.Kits
{
    [Entity]
    public interface IKitDefinition : IProjectRelatedEntity, IMappedToOrderItem
    {
        int Id { get; }

        IEnumerable<IKitSelectionGroup> SelectionGroups { get; }
    }
}
