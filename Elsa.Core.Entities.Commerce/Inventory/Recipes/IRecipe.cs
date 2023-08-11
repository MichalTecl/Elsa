using System;
using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory.Recipes
{
    [Entity]
    public interface IRecipe : IProjectRelatedEntity, IIntIdEntity
    {
        [NVarchar(200, false)]
        string RecipeName { get; set; }

        DateTime ValidFrom { get; set; }

        int CreateUserId { get; set; }
        IUser CreateUser { get; }
        
        int? DeleteUserId { get; set; }
        IUser DeleteUser { get; }

        DateTime? DeleteDateTime { get; set; }

        int ProducedMaterialId { get; set; }
        IMaterial ProducedMaterial { get; }

        decimal RecipeProducedAmount { get; set; }

        decimal? ProductionPricePerUnit { get; set; }

        int ProducedAmountUnitId { get; set; }
        IMaterialUnit ProducedAmountUnit { get; }

        [NVarchar(0, true)]
        string Note { get; set; }

        [NVarchar(1000, true)]
        string VisibleForUserRole { get; set; }

        IEnumerable<IRecipeComponent> Components { get; }
    }
}
