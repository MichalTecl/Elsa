using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.ProductionService.Models;
using Elsa.Apps.ProductionService.Recipes;
using Elsa.Apps.ProductionService.Service;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Commerce.Core.Production.Recipes.Model;
using Elsa.Commerce.Core.VirtualProducts;
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
        private readonly IRecipeService m_recipeService;
        private readonly IMaterialRepository m_materialRepository;

        public ProductionServiceController(IWebSession webSession, ILog log, IRecipeRepository recipeRepository,
            IProductionService productionService, IRecipeService recipeService, IMaterialRepository materialRepository) : base(webSession, log)
        {
            m_recipeRepository = recipeRepository;
            m_productionService = productionService;
            m_recipeService = recipeService;
            m_materialRepository = materialRepository;
        }

        public IEnumerable<RecipeInfo> GetRecipes()
        {
            var result = new List<RecipeInfo>();
            result.AddRange(m_recipeRepository.GetRecipes());

            var manufacturedInventories = m_materialRepository.GetMaterialInventories().Where(i => i.IsManufactured).Select(i => i.Id).ToList();
            result.AddRange(m_materialRepository.GetAllMaterials(null).Where(m => manufacturedInventories.Contains(m.InventoryId)).Where(m => result.All(r => r.MaterialId != m.Id)).OrderBy(m => m.Name).Select(m => new MaterialNodePlaceholder()
            {
                MaterialId = m.Id,
                MaterialName = m.Name
            }));

            return result;
        }

        public RecipeInfo ToggleFavorite(int recipeId)
        {
            var recipe = m_recipeRepository.GetRecipes().FirstOrDefault(r => r.RecipeId == recipeId).Ensure();

            return m_recipeRepository.SetRecipeFavorite(recipeId, !recipe.IsFavorite);
        }

        public RecipeInfo ToggleDeleted(int recipeId)
        {
            var recipe = m_recipeRepository.GetRecipes().FirstOrDefault(r => r.RecipeId == recipeId).Ensure();

            return m_recipeRepository.SetRecipeDeleted(recipeId, recipe.IsActive);
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

        public RecipeEditRequest LoadRecipe(int materialId, int? recipeId)
        {
            return m_recipeService.GetRecipe(materialId, recipeId ?? 0);
        }

        public void SaveRecipe(RecipeEditRequest request)
        {
            m_recipeService.SaveRecipe(request);
        }
    }
}
