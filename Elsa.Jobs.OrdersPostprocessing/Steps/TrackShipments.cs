using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Shipment;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Jobs.OrdersPostprocessing.Steps
{
    public class TrackShipments : ProcessStepBase
    {
        private const int UNWATCH_AFTER_DAYS = 30;

        private readonly IDatabase _database;
        private readonly IShipmentProviderFactory _shipmentProviders;
        private readonly ILog _log;

        public TrackShipments(
            IDatabase database,
            ISession session,
            ILog log,
            IOrdersFacade ordersFacade,
            IShipmentProviderFactory shipmentProviders)
            : base(database, session, log, ordersFacade)
        {
            _database = database;
            _shipmentProviders = shipmentProviders;
            _log = log;
        }

        protected override string ProcessCode => null;

        protected override int? HistoryDepthDays => null;

        protected override IOrderStatus[] SourceOrderStatuses => new[] { OrderStatus.Sent };

        protected override IQueryBuilder<IPurchaseOrder> QueryOrders(IQueryBuilder<IPurchaseOrder> query)
        {
            var activeShipmentOrderIds = _database.SelectFrom<IPurchaseOrderShipmentInfo>()
                .Where(shipment => shipment.Unwatched == null)
                .Transform(shipment => shipment.PurchaseOrderId);

            return query.Where(order => order.Id.InSubquery(activeShipmentOrderIds));
        }

        protected override bool TryProcessOrder(IPurchaseOrder order, Action<string> processingLogMessageWriter)
        {
            var shipments = _database.SelectFrom<IPurchaseOrderShipmentInfo>()
                .Where(shipment => shipment.PurchaseOrderId == order.Id)
                .Execute()
                .ToList();
            var watchedShipments = shipments.Where(shipment => shipment.Unwatched == null).ToList();
            if (watchedShipments.Count == 0)
                return false;

            var changedIds = new HashSet<int>();
            foreach (var shipment in watchedShipments)
            {
                if (string.IsNullOrWhiteSpace(shipment.ShipmentProviderSymbol))
                    throw new InvalidOperationException($"Zásilka {shipment.Id} objednávky {order.OrderNumber} nemá symbol dopravce.");
                if (string.IsNullOrWhiteSpace(shipment.ExternalTrackingNumber))
                    throw new InvalidOperationException($"Zásilka {shipment.Id} objednávky {order.OrderNumber} nemá tracking number.");

                var provider = _shipmentProviders.GetBySymbol(shipment.ShipmentProviderSymbol);
                _log.Info($"Zjišťuji stav zásilky {shipment.ExternalTrackingNumber}, objednávka {order.OrderNumber}, dopravce {provider.Name}.");
                var trackingInfo = provider.GetTrackingInfo(shipment.ExternalTrackingNumber);
                if (trackingInfo == null)
                    throw new InvalidOperationException($"Dopravce {provider.Name} nevrátil stav zásilky {shipment.ExternalTrackingNumber}.");

                if (ApplyTrackingInfo(shipment, trackingInfo))
                    changedIds.Add(shipment.Id);
            }

            var now = DateTime.Now;
            var orderHasDeliveredShipment = shipments.Any(shipment => shipment.Delivered.HasValue);
            var orderHasShipmentInTransport = shipments.Any(shipment => shipment.ParcelTransportStarted.HasValue);
            var oldestWatchedRegistration = now.AddDays(-UNWATCH_AFTER_DAYS);

            foreach (var shipment in watchedShipments)
            {
                var shouldUnwatch = orderHasDeliveredShipment
                                    || (!shipment.ParcelTransportStarted.HasValue && orderHasShipmentInTransport)
                                    || (shipment.ParcelRegistered.HasValue && shipment.ParcelRegistered.Value < oldestWatchedRegistration);
                if (!shouldUnwatch)
                    continue;

                shipment.Unwatched = now;
                changedIds.Add(shipment.Id);
            }

            foreach (var shipment in shipments.Where(shipment => changedIds.Contains(shipment.Id)))
                _database.Save(shipment);

            _log.Info($"Dokončeno sledování objednávky {order.OrderNumber}: sledováno {watchedShipments.Count}, změněno {changedIds.Count}, nově nesledováno {watchedShipments.Count(shipment => shipment.Unwatched.HasValue)}.");
            return true;
        }

        private static bool ApplyTrackingInfo(IPurchaseOrderShipmentInfo shipment, ShipmentTrackingInfo trackingInfo)
        {
            var registered = Earlier(shipment.ParcelRegistered, trackingInfo.ParcelRegistered);
            var transportStarted = Earlier(shipment.ParcelTransportStarted, trackingInfo.ParcelTransportStarted);
            var delivered = Earlier(shipment.Delivered, trackingInfo.Delivered);

            if (delivered.HasValue)
                transportStarted = Earlier(transportStarted, delivered);
            if (transportStarted.HasValue)
                registered = Earlier(registered, transportStarted);

            var changed = shipment.ParcelRegistered != registered
                          || shipment.ParcelTransportStarted != transportStarted
                          || shipment.Delivered != delivered;
            shipment.ParcelRegistered = registered;
            shipment.ParcelTransportStarted = transportStarted;
            shipment.Delivered = delivered;
            return changed;
        }

        private static DateTime? Earlier(DateTime? first, DateTime? second)
        {
            if (!first.HasValue)
                return second;
            if (!second.HasValue)
                return first;
            return first.Value <= second.Value ? first : second;
        }
    }
}
