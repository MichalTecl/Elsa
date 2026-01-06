using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Shipment;
using Elsa.Integration.ShipmentProviders.Zasilkovna.ShipmentRequestDocumentGenerators;
using Robowire;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IShipmentProvider>().Use<ZasilkovnaClient>();
            setup.For<ZasilkovnaClient>().Use<ZasilkovnaClient>();

            setup.For<Zasilkovna4CsvGenerator>().Use<Zasilkovna4CsvGenerator>();
            setup.For<DpdCsvGenerator>().Use<DpdCsvGenerator>();
        }
    }
}
