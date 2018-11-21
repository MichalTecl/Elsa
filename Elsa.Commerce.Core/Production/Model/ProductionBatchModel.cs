using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Production.Model
{
    public class ProductionBatchModel
    {
        public int BatchId { get; set; }

        public string BatchNumber { get; set; }

        public string MaterialName { get; set; }

        public int MaterialId { get; set; }

        public decimal ProducedAmount { get; set; }

        public string ProducedAmountUnitSymbol { get; set; }

        public bool IsComplete { get; set; }

        public List<ProductionBatchComponentModel> Components { get; } = new List<ProductionBatchComponentModel>();

    }
}
