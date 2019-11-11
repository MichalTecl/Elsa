using System.Collections.Generic;
using Elsa.Commerce.Core.Production.Recipes.Model;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;

namespace Elsa.Commerce.Core.Production.Recipes
{
    public interface IRecipeRepository
    {
        IEnumerable<IRecipe> GetRecipesByMaterialId(int materialId);
        
        IRecipe GetRecipe(int recipeId);

        IList<RecipeInfo> GetRecipes();

        RecipeInfo SetRecipeFavorite(int recipeId, bool isFavorite);
    }
}
