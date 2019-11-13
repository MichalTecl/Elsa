using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.ProductionService.Models;
using Elsa.Apps.ProductionService.Service;
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
        private readonly IProductionService m_productionService;

        public ProductionServiceController(IWebSession webSession, ILog log, IRecipeRepository recipeRepository, IProductionService productionService) : base(webSession, log)
        {
            m_recipeRepository = recipeRepository;
            m_productionService = productionService;
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

        public ProductionRequest ValidateProductionRequest(ProductionRequest request)
        {
            m_productionService.ValidateRequest(request.Ensure("Request object required"));

            return request;
        }

        public void ProcessProductionRequest(ProductionRequest request)
        {
            m_productionService.ProcessRequest(request.Ensure("Request object required"));
        }
    }
}
