using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IMaterialComposition
    {
        int Id { get; }

        int CompositionId { get; set; }
        IMaterial Composition { get; }

        int ComponentId { get; set; }
        IMaterial Component { get; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        decimal Amount { get; set; }
    }
}
