using System.Collections.Generic;
using Elsa.Commerce.Core.Production.Recipes.Model;
using Elsa.Commerce.Core.Production.Recipes.Model.RecipeEditing;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;

namespace Elsa.Commerce.Core.Production.Recipes
{
    public interface IRecipeRepository
    {
        IEnumerable<IRecipe> GetRecipesByMaterialId(int materialId);
        
        IRecipe GetRecipe(int recipeId);

        IList<RecipeInfo> GetRecipes();

        RecipeInfo SetRecipeFavorite(int recipeId, bool isFavorite);

        RecipeInfoWithItems LoadRecipe(int recipeId);

        RecipeInfo SetRecipeDeleted(int recipeId, bool shouldBeDeleted);

        RecipeInfo SaveRecipe(int materialId, int recipeId, string recipeName, decimal productionPrice,
            Amount producedAmount, string note, string visibleforUserRole, IEnumerable<RecipeComponentModel> components);
    }
}
