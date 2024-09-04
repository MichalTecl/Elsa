using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Production.Recipes.Model
{
    public class RecipeInfo
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; }

        public int RecipeId { get; set; }

        public string RecipeName { get; set; }

        public bool IsActive { get; set; }

        public bool IsFavorite { get; set; }

        public string VisibleForUserRole { get; set; }

        public bool AllowOneClickProduction { get; set; }
    }
}
