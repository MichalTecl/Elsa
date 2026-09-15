using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Shipment
{
    public interface IShipmentProviderFactory
    {
        IShipmentProvider GetBySymbol(string symbol);

        /// <summary>
        /// "Uses ShipmentProviderLookup table to find shipment provider by shipment method.
        /// Throws an exception if not found.
        /// </summary>
        /// <param name="shipmentMethodText"></param>
        /// <returns></returns>
        IShipmentProvider GetByShipmentMethod(string shipmentMethodText);
    }
}
