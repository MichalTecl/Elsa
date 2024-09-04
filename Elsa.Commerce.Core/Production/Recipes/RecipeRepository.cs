using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Elsa.Commerce.Core.Production.Recipes.Model;
using Elsa.Commerce.Core.Production.Recipes.Model.RecipeEditing;
using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;
using Elsa.Users;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Production.Recipes
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly IDatabase m_database;
        private readonly ICache m_cache;
        private readonly ISession m_session;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IUnitConversionHelper m_conversionHelper;
        private readonly IUserRoleRepository m_userRepository;

        public RecipeRepository(IDatabase database, ICache cache, ISession session, IMaterialRepository materialRepository, IUnitRepository unitRepository, IUnitConversionHelper conversionHelper, IUserRoleRepository userRepository)
        {
            m_database = database;
            m_cache = cache;
            m_session = session;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_conversionHelper = conversionHelper;
            m_userRepository = userRepository;
        }

        public IEnumerable<IRecipe> GetRecipesByMaterialId(int materialId)
        {
            throw new NotImplementedException();
        }

        public IRecipe GetRecipe(int recipeId)
        {
            //TODO cache
            return m_database.SelectFrom<IRecipe>()
                .Join(r => r.Components)
                .Where(r => r.ProjectId == m_session.Project.Id)
                .Where(r => r.Id == recipeId)
                .OrderBy(r => r.Components.Each().SortOrder).Execute().FirstOrDefault();
        }

        public IList<RecipeInfo> GetRecipes()
        {
            return m_cache.ReadThrough($"recipes{m_session.User.Id}", TimeSpan.FromSeconds(10), () =>
            {
                var favorites = new HashSet<int>(m_database.SelectFrom<IUserFavoriteRecipe>()
                    .Where(r => r.UserId == m_session.User.Id).Transform(r => r.RecipeId).Execute());

                var dbRecipes = m_database.SelectFrom<IRecipe>().Where(r => r.ProjectId == m_session.Project.Id)                    
                    .Execute();

                var materials = m_materialRepository.GetAllMaterials(null).ToArray();

                var result = new List<RecipeInfo>(dbRecipes.Where(FilterByUserRoleVisibility).Select(e => new RecipeInfo
                {
                    IsActive = e.DeleteUserId == null,
                    IsFavorite = favorites.Contains(e.Id),
                    MaterialId = e.ProducedMaterialId,
                    MaterialName = materials.FirstOrDefault(mm => mm.Id == e.ProducedMaterialId)?.Name,
                    RecipeId = e.Id,
                    RecipeName = e.RecipeName                   
                }).OrderBy(r => r.MaterialName).ThenBy(r => r.IsActive ? 0 : 1).ThenBy(r => r.IsFavorite ? 0 : 1).ThenBy(r => r.RecipeName));

                return result;
            });
        }

        private bool FilterByUserRoleVisibility(IRecipe arg)
        {
            if (m_session.HasUserRight("ViewAllReceptures"))
                return true;

            if (string.IsNullOrWhiteSpace(arg.VisibleForUserRole))
                return false;

            var roleMap = m_userRepository.GetProjectRoles();
            var usersRoles = roleMap.FindRolesByUserId(m_session.User.Id);

            return usersRoles.Any(ur => ur.Name == arg.VisibleForUserRole);
        }

        public RecipeInfo SetRecipeFavorite(int recipeId, bool isFavorite)
        {
            var entity = m_database.SelectFrom<IUserFavoriteRecipe>()
                .Where(f => f.RecipeId == recipeId && f.UserId == m_session.User.Id).Take(1).Execute().FirstOrDefault();
            
            if (isFavorite && (entity == null))
            {
                entity = m_database.New<IUserFavoriteRecipe>(f =>
                {
                    f.RecipeId = recipeId;
                    f.UserId = m_session.User.Id;
                });

                m_database.Save(entity);
            }

            if ((!isFavorite) && (entity != null))
            {
                m_database.Delete(entity);
            }

            m_cache.Remove($"recipes{m_session.User.Id}");

            return GetRecipes().FirstOrDefault(r => r.RecipeId == recipeId);
        }

        public RecipeInfoWithItems LoadRecipe(int recipeId)
        {
            var entity = m_database.SelectFrom<IRecipe>().Join(r => r.Components).Join(r => r.ProducedMaterial)
                .Join(r => r.Components.Each().Material)
                .Where(r => r.ProjectId == m_session.Project.Id && r.Id == recipeId).Execute().FirstOrDefault();

            if (entity == null)
            {
                return null;
            }

            var model = new RecipeInfoWithItems()
            {
                IsActive = entity.DeleteDateTime == null && entity.DeleteUserId == null,
                MaterialId = entity.ProducedMaterialId,
                MaterialName = entity.ProducedMaterial.Name,
                RecipeId = entity.Id,
                RecipeName = entity.RecipeName,
                Amount = entity.RecipeProducedAmount,
                ProductionPrice = entity.ProductionPricePerUnit ?? 0,
                AmountUnit = m_unitRepository.GetUnit(entity.ProducedAmountUnitId).Symbol,
                VisibleForUserRole = entity.VisibleForUserRole,
                Note = entity.Note,
                AllowOneClickProduction = entity.AllowOneClickProduction ?? false
            };

            foreach (var c in entity.Components.OrderBy(c => c.SortOrder))
            {
                var me = new MaterialEntry()
                {
                    Amount = c.Amount,
                    MaterialName = c.Material.Name,
                    UnitName = m_unitRepository.GetUnit(c.UnitId).Symbol
                };

                model.Items.Add(new RecipeItem()
                {
                    IsTransformationSource = c.IsTransformationInput,
                    Text = me.ToString(),
                    IsValid = true
                });
            }

            return model;
        }

        public RecipeInfo SetRecipeDeleted(int recipeId, bool shouldBeDeleted)
        {
            var entity = m_database.SelectFrom<IRecipe>()
                .Where(r => r.ProjectId == m_session.Project.Id && r.Id == recipeId).Take(1).Execute().FirstOrDefault().Ensure();

            if (shouldBeDeleted == (entity.DeleteUserId != null))
            {
                return GetRecipes().FirstOrDefault(r => r.RecipeId == recipeId);
            }

            if (shouldBeDeleted)
            {
                entity.DeleteUserId = m_session.User.Id;
                entity.DeleteDateTime = DateTime.Now;
            }
            else
            {
                entity.DeleteUserId = null;
                entity.DeleteDateTime = null;
            }

            m_database.Save(entity);

            m_cache.Remove($"recipes{m_session.User.Id}");

            return GetRecipes().FirstOrDefault(r => r.RecipeId == recipeId);
        }

        public RecipeInfo SaveRecipe(int materialId, int recipeId, string recipeName, decimal productionPrice,
            Amount producedAmount,
            string note, string visibleForUserRole, bool allowOneClickProduction, IEnumerable<RecipeComponentModel> components)
        {
            using (var tx = m_database.OpenTransaction())
            {
                var recipeMaterial = m_materialRepository.GetMaterialById(materialId).Ensure();
                if (!recipeMaterial.IsManufactured)
                {
                    throw new InvalidOperationException(
                        $"Material {recipeMaterial.Name} není nastaven jako vyráběný (podle skladu {recipeMaterial.InventoryName}, do kterého patří)");
                }

                var allMaterialRecipes = m_database.SelectFrom<IRecipe>().Join(r => r.Components)
                    .Where(r => r.ProjectId == m_session.Project.Id)
                    .Where(r => r.ProducedMaterialId == materialId).Execute().ToList();

                if (allMaterialRecipes.Any(r =>
                    r.Id != recipeId && r.RecipeName.Equals(recipeName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        $"Pro \"{recipeMaterial.Name}\" již existuje receptura s názvem \"{recipeName}\"");
                }

                if (!m_conversionHelper.AreCompatible(recipeMaterial.NominalUnit.Id, producedAmount.Unit.Id))
                {
                    throw new InvalidOperationException(
                        $"Pro \"{recipeMaterial.Name}\" nelze použít jednotku \"{producedAmount.Unit.Symbol}\"");
                }

                if (producedAmount.IsNotPositive)
                {
                    throw new InvalidOperationException("Výsledné množství musí být kladné číslo");
                }

                var entity = allMaterialRecipes.FirstOrDefault(r => r.Id == recipeId);

                if ((entity == null) && (recipeId > 0))
                {
                    throw new InvalidOperationException("!");
                }

                entity = entity ?? m_database.New<IRecipe>(r =>
                {
                    r.ValidFrom = DateTime.Now;
                    r.CreateUserId = m_session.User.Id;
                    r.ProjectId = m_session.Project.Id;
                });

                entity.RecipeName = recipeName;
                entity.ProducedMaterialId = recipeMaterial.Id;
                entity.RecipeProducedAmount = producedAmount.Value;
                entity.ProducedAmountUnitId = producedAmount.Unit.Id;
                entity.ProductionPricePerUnit = productionPrice > 0 ? (decimal?)productionPrice : null;
                entity.Note = note;
                entity.VisibleForUserRole = visibleForUserRole;
                entity.AllowOneClickProduction = allowOneClickProduction;

                m_database.Save(entity);

                var transfSrcFound = false;
                var usedMaterials = new HashSet<int>();
                
                foreach (var srcComponent in components)
                {
                    if (srcComponent.IsTransformationSource)
                    {
                        if (transfSrcFound)
                        {
                            throw new InvalidOperationException($"Pouze jedna slozka muze byt hlavni");
                        }

                        transfSrcFound = true;
                    }

                    var componetMaterial = m_materialRepository.GetMaterialById(srcComponent.MaterialId).Ensure();

                    if (!srcComponent.Amount.IsPositive)
                    {
                        throw new InvalidOperationException($"Množství složky {componetMaterial.Name} musí být kladné číslo");
                    }

                    if (!m_conversionHelper.AreCompatible(componetMaterial.NominalUnit.Id, srcComponent.Amount.Unit.Id))
                    {
                        throw new InvalidOperationException(
                            $"Pro \"{componetMaterial.Name}\" nelze použít jednotku \"{srcComponent.Amount.Unit.Symbol}\"");
                    }

                    if (!usedMaterials.Add(srcComponent.MaterialId))
                    {
                        throw new InvalidOperationException($"Materiál {componetMaterial.Name} je ve složení více než jednou");
                    }


                    var componentEntity =
                        entity.Components.FirstOrDefault(c => c.MaterialId == srcComponent.MaterialId) ??
                        m_database.New<IRecipeComponent>(
                            c =>
                            {
                                c.MaterialId = srcComponent.MaterialId;
                                c.RecipeId = entity.Id;
                            });

                    componentEntity.IsTransformationInput = srcComponent.IsTransformationSource;
                    componentEntity.MaterialId = componetMaterial.Id;
                    componentEntity.SortOrder = srcComponent.SortOrder;
                    componentEntity.Amount = srcComponent.Amount.Value;
                    componentEntity.UnitId = srcComponent.Amount.Unit.Id;

                    m_database.Save(componentEntity);
                }

                if (!usedMaterials.Any())
                {
                    throw new InvalidOperationException("Receptura musí mít alespoň jednu složku!");
                }

                if(!transfSrcFound && allowOneClickProduction)
                {
                    throw new InvalidOperationException("Receptura umožňující výrobu jedním kliknutím musí mít hlavní složku");
                }

                foreach (var removedComponent in entity.Components.Where(rc => !usedMaterials.Contains(rc.MaterialId)))
                {
                    m_database.Delete(removedComponent);
                }

                tx.Commit();

                m_cache.Remove($"recipes{m_session.User.Id}");

                return LoadRecipe(entity.Id);
            }
        }
    }
}
