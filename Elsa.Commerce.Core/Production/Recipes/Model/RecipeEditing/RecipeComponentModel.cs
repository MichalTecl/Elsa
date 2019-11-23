using Elsa.Common;

namespace Elsa.Commerce.Core.Production.Recipes.Model.RecipeEditing
{
    public class RecipeComponentModel
    {
        public int MaterialId { get; set; }
        public int SortOrder { get; set; }
        public Amount Amount { get; set; }
        public bool IsTransformationSource { get; set; }
    }
}
