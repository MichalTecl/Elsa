using Robowire;

namespace Elsa.Integration.ShipmentProviders.Dpd
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<DpdTrackingClient>().Use<DpdTrackingClient>();
            setup.For<DpdShipmentProvider>().Use<DpdShipmentProvider>();
        }
    }
}
