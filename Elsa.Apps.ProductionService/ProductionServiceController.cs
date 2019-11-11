using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Commerce.Core.Production.Recipes.Model;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RoboApi;

namespace Elsa.Apps.ProductionService
{
    [Controller("ProductionService")]
    public class ProductionServiceController : ElsaControllerBase
    {
        private readonly IRecipeRepository m_recipeRepository;

        public ProductionServiceController(IWebSession webSession, ILog log, IRecipeRepository recipeRepository) : base(webSession, log)
        {
            m_recipeRepository = recipeRepository;
        }

        public IEnumerable<RecipeInfo> GetRecipes()
        {
            return m_recipeRepository.GetRecipes();
        }

        public RecipeInfo ToggleFavorite(int recipeId)
        {
            var recipe = m_recipeRepository.GetRecipes().FirstOrDefault(r => r.RecipeId == recipeId).Ensure();

            return m_recipeRepository.SetRecipeFavorite(recipeId, !recipe.IsFavorite);
        }
    }
}
