namespace Elsa.Commerce.Core.Production.Recipes.Model.RecipeEditing
{
    public class RecipeItem
    {
        public bool IsTransformationSource { get; set; }

        public string Text { get; set; }

        public bool IsValid { get; set; }

        public string Error { get; set; }
    }
}
