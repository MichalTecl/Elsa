using System;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common.Utils;

namespace Elsa.Apps.ProductionService.Service.Process.Steps
{
    internal class LoadRecipe : IProductionRequestProcessingStep
    {
        private readonly IRecipeRepository m_recipeRepository;
        private readonly IMaterialRepository m_materialRepository;

        public LoadRecipe(IRecipeRepository recipeRepository, IMaterialRepository materialRepository)
        {
            m_recipeRepository = recipeRepository;
            m_materialRepository = materialRepository;
        }

        public void Process(ProductionRequestContext context)
        {
            var recipe = m_recipeRepository.GetRecipe(context.Request.RecipeId).Ensure();

            if (recipe.DeleteUser != null || recipe.DeleteDateTime != null)
            {
                throw new InvalidOperationException($"Receptura {recipe.RecipeName} není aktivní");
            }

            context.Request.ProdPricePerUnit = recipe.ProductionPricePerUnit;
            context.Request.RecipeNote = recipe.Note;
            context.Recipe = recipe;
            context.Request.RecipeName = recipe.RecipeName;
            context.TargetMaterial = m_materialRepository.GetMaterialById(recipe.ProducedMaterialId)?.Adaptee;
        }
    }
}
