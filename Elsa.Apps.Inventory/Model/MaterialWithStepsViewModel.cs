using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.Inventory.Model
{
    public class MaterialWithStepsViewModel
    {
        public int MaterialId { get; set; }

        public bool IsAutoBatch { get; set; }

        public string MaterialName { get; set; }

        public List<ProductionStepHandleModel> Steps { get; } = new List<ProductionStepHandleModel>();

        public void AddStep(int stepId, string name)
        {
            Steps.Add(new ProductionStepHandleModel { MaterialProductionStepId = stepId, Name = name });
        }

        public class ProductionStepHandleModel
        {
            public int MaterialProductionStepId { get; set; }

            public string Name { get; set; }
        }

        
    }


}
