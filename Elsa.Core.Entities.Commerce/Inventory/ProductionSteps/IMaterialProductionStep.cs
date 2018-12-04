using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory.ProductionSteps
{
    [Entity]
    public interface IMaterialProductionStep 
    {
        int Id { get; }
        
        [NVarchar(250, false)]
        string Name { get; set; }

        int MaterialId { get; set; }
        IMaterial Material { get; }
        
        int? PreviousStepId { get; set; }
        IMaterialProductionStep PreviousStep { get; }

        bool RequiresPrice { get; set; }
        bool RequiresSpentTime { get; set; }
        bool RequiresWorkerReference { get; set; }

        [ForeignKey(nameof(IMaterialProductionStepMaterial.StepId))]
        IEnumerable<IMaterialProductionStepMaterial> Components { get; }
    }
}
