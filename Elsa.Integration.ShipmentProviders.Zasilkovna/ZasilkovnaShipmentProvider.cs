using Elsa.Commerce.Core.Shipment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    public class ZasilkovnaShipmentProvider : IShipmentProvider
    {
        private readonly ZasilkovnaClientConfig _config;
        private readonly PacketaTrackingClient _trackingClient;
        private readonly object _sync = new object();
        private Dictionary<string, HashSet<string>> _shipments;
        private DateTime _loadedAt;

        public ZasilkovnaShipmentProvider(ZasilkovnaClientConfig config, PacketaTrackingClient trackingClient)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _trackingClient = trackingClient ?? throw new ArgumentNullException(nameof(trackingClient));
        }

        public string Symbol => "zasilkovna";

        public string Name => "Zásilkovna";

        public IEnumerable<string> TryGetExternalShipmentReferences(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("Reference objednávky nesmí být prázdná.", nameof(orderNumber));

            lock (_sync)
            {
                if (_shipments == null || DateTime.UtcNow - _loadedAt >= TimeSpan.FromMinutes(5))
                {
                    // Publish only a complete snapshot; a failed page must not look like a missing shipment.
                    _shipments = PacketaWebClient.LoadShipments(_config);
                    _loadedAt = DateTime.UtcNow;
                }

                if (!_shipments.TryGetValue(orderNumber.Trim(), out var numbers))
                    return new string[0];

                return numbers.OrderBy(number => number, StringComparer.Ordinal).ToArray();
            }
        }

        public ShipmentTrackingInfo GetTrackingInfo(string externalTrackingNumber)
        {
            return _trackingClient.GetTrackingInfo(externalTrackingNumber);
        }
    }
}
