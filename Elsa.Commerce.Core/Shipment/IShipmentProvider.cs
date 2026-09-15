using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Shipment
{
    public interface IShipmentProvider
    {
        string Symbol { get; }

        string Name { get; }

        /// <summary>
        /// Finds external tracking numbers by the customer reference exported to the carrier.
        /// </summary>
        /// <param name="orderNumber">The value returned by IErpClient.GetPackingReferenceNumber, as used in shipment exports.</param>
        /// <returns>External tracking numbers. An empty collection means that no shipment is available yet. Provider failures must throw.</returns>
        IEnumerable<string> TryGetExternalShipmentReferences(string orderNumber);

        /// <summary>
        /// Returns known lifecycle milestones for an external tracking number.
        /// Provider failures and malformed responses must throw.
        /// </summary>
        ShipmentTrackingInfo GetTrackingInfo(string externalTrackingNumber);
    }
}
