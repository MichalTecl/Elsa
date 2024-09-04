using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.ProductionService.Models;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Production.Recipes;
using Elsa.Commerce.Core.Production.Recipes.Model.RecipeEditing;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common;
using Elsa.Common.Utils;

namespace Elsa.Apps.ProductionService.Recipes
{
    public class RecipeService : IRecipeService
    {
        private readonly IRecipeRepository m_recipes;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;

        public RecipeService(IRecipeRepository recipes, IMaterialRepository materialRepository, IUnitRepository unitRepository)
        {
            m_recipes = recipes;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
        }
        
        public RecipeEditRequest GetRecipe(int materialId, int recipeId)
        {
            var material = m_materialRepository.GetMaterialById(materialId).Ensure();
            var entity = recipeId < 1 ? null : m_recipes.LoadRecipe(recipeId).Ensure();

            var model = new RecipeEditRequest
            {
                MaterialId = material.Id,
                MaterialName = material.Name,
                Amount = material.NominalAmount,
                IsActive = true,
                RecipeName = "Výroba",
                AmountUnit = material.NominalUnit.Symbol
            };

            if (entity != null)
            {
                if (entity.MaterialId != model.MaterialId)
                {
                    throw new InvalidOperationException("Nelze zmenit material existujici receptury");
                }

                model.Amount = entity.Amount;
                model.AmountUnit = entity.AmountUnit;
                model.RecipeId = entity.RecipeId;
                model.IsActive = entity.IsActive;
                model.Note = entity.Note;
                model.RecipeName = entity.RecipeName;
                model.ProductionPrice = entity.ProductionPrice;
                model.VisibleForUserRole = entity.VisibleForUserRole;
                model.AllowOneClickProduction = entity.AllowOneClickProduction;

                model.Items.AddRange(entity.Items);
            }

            var mentry = new MaterialEntry()
            {
                Amount = model.Amount,
                UnitName = model.AmountUnit
            };

            model.ProducedAmountText = mentry.ToString();

            return model;
        }

        public void SaveRecipe(RecipeEditRequest rq)
        {
            if (string.IsNullOrWhiteSpace(rq.ProducedAmountText))
            {
                throw new InvalidOperationException("Vyráběné množství je třeba zadat");
            }

            if (string.IsNullOrWhiteSpace(rq.RecipeName))
            {
                throw new InvalidOperationException("Název receptury musí být zadán");
            }

            var recipeMaterial = m_materialRepository.GetMaterialById(rq.MaterialId).Ensure();
            var recipeAmount = MaterialEntry.Parse(rq.ProducedAmountText, true).GetAmount(m_unitRepository);
            
            var items = new List<RecipeComponentModel>();

            foreach (var srcItem in rq.Items)
            {
                if (string.IsNullOrWhiteSpace(srcItem.Text))
                {
                    continue;
                }

                try
                {
                    var entry = MaterialEntry.Parse(srcItem.Text);
                
                    var componentMaterial = m_materialRepository.GetMaterialByName(entry.MaterialName);
                    if (componentMaterial == null)
                    {
                        throw new InvalidOperationException($"Neznámý materiál \"{entry.MaterialName}\"");
                    }

                    if (componentMaterial.Id == recipeMaterial.Id)
                    {
                        throw new InvalidOperationException($"Receptura nesmí obsahovat ve složení svůj vlastní výsledný materiál \"{entry.MaterialName}\"");
                    }
                    
                    var model = new RecipeComponentModel
                    {
                        IsTransformationSource = srcItem.IsTransformationSource,
                        SortOrder = items.Count,
                        MaterialId = componentMaterial.Id,
                        Amount = entry.GetAmount(m_unitRepository)
                    };

                    items.Add(model);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Chybné zadání složky \"{srcItem.Text}\": {e.Message}", e);
                }
            }

            m_recipes.SaveRecipe(recipeMaterial.Id, rq.RecipeId, rq.RecipeName.Trim(), rq.ProductionPrice, recipeAmount,
                rq.Note?.Trim(), rq.VisibleForUserRole, rq.AllowOneClickProduction, items);
        }
    }
}
