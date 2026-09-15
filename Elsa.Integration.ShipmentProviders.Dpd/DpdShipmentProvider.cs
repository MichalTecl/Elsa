using Elsa.Commerce.Core.Shipment;
using System;
using System.Collections.Generic;

namespace Elsa.Integration.ShipmentProviders.Dpd
{
    public class DpdShipmentProvider : IShipmentProvider
    {
        private readonly DpdTrackingClient _trackingClient;

        public DpdShipmentProvider(DpdTrackingClient trackingClient)
        {
            _trackingClient = trackingClient ?? throw new ArgumentNullException(nameof(trackingClient));
        }

        public string Symbol => "dpd";

        public string Name => "DPD";

        public IEnumerable<string> TryGetExternalShipmentReferences(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("Reference objednávky nesmí být prázdná.", nameof(orderNumber));

            return _trackingClient.FindParcelNumbers(orderNumber.Trim());
        }

        public ShipmentTrackingInfo GetTrackingInfo(string externalTrackingNumber)
        {
            return _trackingClient.GetTrackingInfo(externalTrackingNumber);
        }
    }
}
