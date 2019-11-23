using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.ProductionService.Models;
using Elsa.Commerce.Core.Production.Recipes.Model.RecipeEditing;

namespace Elsa.Apps.ProductionService.Recipes
{
    public interface IRecipeService
    {
        RecipeEditRequest GetRecipe(int materialId, int recipeId);

        void SaveRecipe(RecipeEditRequest rq);
    }
}
