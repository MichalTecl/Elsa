using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.ProductionSteps
{
    [Entity]
    public interface IMaterialProductionStepMaterial
    {
        int Id { get; }

        int StepId { get; set; }
        IMaterialProductionStep Step { get; }

        int MaterialId { get; set; }
        IMaterial Material { get; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        decimal Amount { get; set; }
    }
}
