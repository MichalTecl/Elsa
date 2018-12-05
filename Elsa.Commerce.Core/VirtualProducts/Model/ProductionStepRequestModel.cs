using System.Collections.Generic;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    public class ProductionStepRequestModel
    {
        public int StepOrder { get; set; }

        public int? StepId { get; set; }

        public string StepName { get; set; }

        public bool Price { get; set; }

        public bool Time { get; set; }

        public bool Worker { get; set; }

        public List<ProductionStepMaterialRequestModel> Materials { get; set; } = new List<ProductionStepMaterialRequestModel>();
    }

}
