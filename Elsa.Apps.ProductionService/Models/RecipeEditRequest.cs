using Elsa.Commerce.Core.Production.Recipes.Model.RecipeEditing;

namespace Elsa.Apps.ProductionService.Models
{
    public class RecipeEditRequest : RecipeInfoWithItems
    {
        public bool IsValid { get; set; }

        public string ProducedAmountText { get; set; }

        public string Message { get; set; }
    }
}
