using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Commerce.Core.Production.Recipes.Model;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Inventory.Recipes;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Production.Recipes
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly IDatabase m_database;
        private readonly ICache m_cache;
        private readonly ISession m_session;
        private readonly IMaterialRepository m_materialRepository;
        
        public RecipeRepository(IDatabase database, ICache cache, ISession session, IMaterialRepository materialRepository)
        {
            m_database = database;
            m_cache = cache;
            m_session = session;
            m_materialRepository = materialRepository;
        }

        public IEnumerable<IRecipe> GetRecipesByMaterialId(int materialId)
        {
            throw new NotImplementedException();
        }

        public IRecipe GetRecipe(int recipeId)
        {
            throw new NotImplementedException();
        }

        public IList<RecipeInfo> GetRecipes()
        {
            return m_cache.ReadThrough($"recipes{m_session.User.Id}", TimeSpan.FromMinutes(20), () =>
            {
                var favorites = new HashSet<int>(m_database.SelectFrom<IUserFavoriteRecipe>()
                    .Where(r => r.UserId == m_session.User.Id).Transform(r => r.RecipeId).Execute());

                var dbRecipes = m_database.SelectFrom<IRecipe>().Where(r => r.ProjectId == m_session.Project.Id)
                    .Execute();

                var materials = m_materialRepository.GetAllMaterials(null).ToArray();

                var result = new List<RecipeInfo>(dbRecipes.Select(e => new RecipeInfo
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
    }
}
