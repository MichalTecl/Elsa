using Elsa.Commerce.Core.Production.Recipes.Model;

namespace Elsa.Apps.ProductionService.Models
{
    public class MaterialNodePlaceholder : RecipeInfo
    {
        public MaterialNodePlaceholder()
        {
            IsActive = true;
        }

        public bool IsPlaceholder => true;
    }
}
