using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory.Recipes
{
    [Entity]
    public interface IRecipeComponent : IIntIdEntity, IAmountAndUnit
    {
        int MaterialId { get; set; }
        IMaterial Material { get; }

        int SortOrder { get; set; }

        int RecipeId { get; set; }
        IRecipe Recipe { get; }

        bool IsTransformationInput { get; set; }
    }
}
