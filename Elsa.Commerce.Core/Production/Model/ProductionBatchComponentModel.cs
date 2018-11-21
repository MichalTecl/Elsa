using System.Collections.Generic;

namespace Elsa.Commerce.Core.Production.Model
{
    public class ProductionBatchComponentModel
    {
        public int MaterialId { get; set; }

        public int MaterialCompositionId { get; set; }

        public string MaterialName { get; set; }

        public decimal RequiredAmount { get; set; }

        public string RequiredAmountUnitSymbol { get; set; }

        public List<SubBatchAssignmentModel> Assignments { get; } = new List<SubBatchAssignmentModel>();
    }
}
