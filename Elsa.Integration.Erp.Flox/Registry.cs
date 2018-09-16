using Robowire;

namespace Elsa.Integration.Erp.Flox
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<FloxDataMapper>().Use<FloxDataMapper>();
        }
    }
}
