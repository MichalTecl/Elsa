using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Recipes
{
    [Entity]
    public interface IUserFavoriteRecipe : IIntIdEntity
    {
        int RecipeId { get; set; }
        IRecipe Recipe { get; }

        int UserId { get; set; }
        IUser User { get; }
    }
}

