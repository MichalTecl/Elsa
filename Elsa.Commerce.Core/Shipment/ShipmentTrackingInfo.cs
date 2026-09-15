using System;

namespace Elsa.Commerce.Core.Shipment
{
    public class ShipmentTrackingInfo
    {
        public DateTime? ParcelRegistered { get; set; }

        public DateTime? ParcelTransportStarted { get; set; }

        public DateTime? Delivered { get; set; }
    }
}
