using System.Collections.Generic;

namespace Elsa.Commerce.Core.Production.Recipes.Model.RecipeEditing
{
    public class RecipeInfoWithItems : RecipeInfo
    {
        public string Note { get; set; }

        public decimal Amount { get; set; }

        public decimal ProductionPrice { get; set; }

        public string AmountUnit { get; set; }

        public List<RecipeItem> Items { get; } = new List<RecipeItem>();
    }
}
