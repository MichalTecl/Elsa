using Elsa.Apps.ProductionService.Recipes;
using Elsa.Apps.ProductionService.Service;
using Robowire;

namespace Elsa.Apps.ProductionService
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IProductionService>().Use<Service.ProductionService>();
            setup.For<IRecipeService>().Use<RecipeService>();
        }
    }
}
