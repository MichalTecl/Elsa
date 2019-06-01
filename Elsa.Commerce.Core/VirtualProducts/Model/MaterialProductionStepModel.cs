using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.ProductionSteps;

using Newtonsoft.Json;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public class MaterialProductionStepModel
    {
        public MaterialProductionStepModel(IMaterialProductionStep entity)
        {
            Adaptee = entity;
            Id = entity.Id;
            PreviousStepId = entity.PreviousStepId;
            StepName = entity.Name;
            RequiresPrice = entity.RequiresPrice;
            RequiresSpentTime = entity.RequiresSpentTime;
            RequiresWorkerReference = entity.RequiresWorkerReference;
            PricePerUnit = entity.PricePerUnit ?? 0;
            
            Components = entity.Components.Select(c => new MaterialProductionStepMaterialModel(c)).ToList();
        }

        [JsonIgnore]
        public IMaterialProductionStep Adaptee { get; }

        public int Id { get; }

        public int? PreviousStepId { get; }

        public string StepName { get; }

        public bool RequiresPrice { get; }

        public bool RequiresSpentTime { get; }

        public bool RequiresWorkerReference { get; }

        public List<MaterialProductionStepMaterialModel> Components { get; }

        public decimal PricePerUnit { get; set; }
    }
}
