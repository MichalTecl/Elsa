using Elsa.Jobs.AutomaticQueries.Components;
using Robowire;

namespace Elsa.Jobs.AutomaticQueries
{
    public class AutoQueriesRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IParametersResolver>().Use<ParametersResolver>();
            setup.For<AutoProceduresJob>().Use<AutoProceduresJob>();
        }
    }
}
