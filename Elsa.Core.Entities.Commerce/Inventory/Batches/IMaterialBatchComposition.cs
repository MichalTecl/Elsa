using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Batches
{
    [Entity]
    public interface IMaterialBatchComposition
    {
        int Id { get; }

        int CompositionId { get; set; }
        IMaterialBatch Composition { get; }

        int ComponentId { get; set; }
        IMaterialBatch Component { get; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        decimal Volume { get; set; }
    }
}
