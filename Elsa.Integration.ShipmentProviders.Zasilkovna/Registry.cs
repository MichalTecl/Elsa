using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Shipment;

using Robowire;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IShipmentProvider>().Use<ZasilkovnaClient>();
        }
    }
}
