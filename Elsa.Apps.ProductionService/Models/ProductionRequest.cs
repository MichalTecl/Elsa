using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ProductionService.Models
{
    public class ProductionRequest
    {
        public int RecipeId { get; set; }

        public string MaterialName { get; set; }

        public string RecipeNote { get; set; }

        public string ProducingBatchNumber { get; set; }

        public string ProducingUnitSymbol { get; set; }

        public decimal? ProducingAmount { get; set; }

        public decimal? ProducingPrice { get; set; }

        public decimal? ProdPricePerUnit { get; set; }

        public bool IsValid { get; set; }

        public List<RequestValidationMessage> Messages { get; } = new List<RequestValidationMessage>();

        public List<ProductionComponent> Components { get; } = new List<ProductionComponent>();
        public decimal PriceCalcAmount { get; set; }
    }
}
