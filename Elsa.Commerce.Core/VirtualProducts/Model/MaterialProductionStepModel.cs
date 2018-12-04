using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public class MaterialProductionStepModel
    {
        public MaterialProductionStepModel(IMaterialProductionStep entity)
        {
            Id = entity.Id;
            PreviousStepId = entity.PreviousStepId;
            StepName = entity.Name;
            RequiresPrice = entity.RequiresPrice;
            RequiresSpentTime = entity.RequiresSpentTime;
            RequiresWorkerReference = entity.RequiresWorkerReference;

            Components = entity.Components.Select(c => new MaterialProductionStepMaterialModel(c)).ToList();
        }

        public int Id { get; }

        public int? PreviousStepId { get; }

        public string StepName { get; }

        public bool RequiresPrice { get; }

        public bool RequiresSpentTime { get; }

        public bool RequiresWorkerReference { get; }

        public List<MaterialProductionStepMaterialModel> Components { get; }
    }
}
