using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Shipment;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Robowire.RobOrm.Core;
using System;
using System.Linq;

namespace Elsa.Jobs.OrdersPostprocessing.Steps
{
    public class ObtainShipmentInfo : ProcessStepBase
    {
        private const int MAX_TRACKING_NUMBER_LENGTH = 100;

        private readonly IShipmentProviderFactory _shipmentProviders;
        private readonly IErpClientFactory _erpClients;
        private readonly IDatabase _database;
        private readonly ILog _log;

        public ObtainShipmentInfo(
            IDatabase database,
            ISession session,
            ILog log,
            IOrdersFacade ordersFacade,
            IShipmentProviderFactory shipmentProviders,
            IErpClientFactory erpClients)
            : base(database, session, log, ordersFacade)
        {
            _database = database;
            _log = log;
            _shipmentProviders = shipmentProviders;
            _erpClients = erpClients;
        }

        protected override string ProcessCode => OrderProcessingCodes.GOT_SHIPMENT_INFO;

        protected override int? HistoryDepthDays => 60;

        protected override IOrderStatus[] SourceOrderStatuses => new[] { OrderStatus.Sent };

        protected override IQueryBuilder<IPurchaseOrder> QueryOrders(IQueryBuilder<IPurchaseOrder> query)
        {
            return query;
        }

        protected override bool TryProcessOrder(IPurchaseOrder order, Action<string> processingLogMessageWriter)
        {            
            if (!order.ErpId.HasValue)
                throw new InvalidOperationException($"Objednávka {order.OrderNumber} nemá přiřazený ERP systém.");

            var provider = _shipmentProviders.GetByShipmentMethod(order.ShippingMethodName);

            // Use the same customer reference that the shipment CSV exports to the carrier.
            var orderReference = _erpClients.GetErpClient(order.ErpId.Value).GetPackingReferenceNumber(order);
            if (string.IsNullOrWhiteSpace(orderReference))
                throw new InvalidOperationException($"Objednávka {order.OrderNumber} nemá referenci pro dopravce {provider.Name}.");

            _log.Info($"Získávám číslo zásilky pro objednávku {order.OrderNumber}, dopravce {provider.Name}, reference {orderReference}.");
            var externalNumbers = provider.TryGetExternalShipmentReferences(orderReference)?
                .Where(number => !string.IsNullOrWhiteSpace(number))
                .Select(number => number.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(number => number, StringComparer.Ordinal)
                .ToList();

            if (externalNumbers == null || externalNumbers.Count == 0)
            {
                _log.Info($"Dopravce {provider.Name} zatím nevrátil číslo zásilky pro objednávku {order.OrderNumber}");
                return false;
            }

            var tooLongNumber = externalNumbers.FirstOrDefault(number => number.Length > MAX_TRACKING_NUMBER_LENGTH);
            if (tooLongNumber != null)
                throw new InvalidOperationException($"Číslo zásilky od dopravce {provider.Name} překračuje povolenou délku {MAX_TRACKING_NUMBER_LENGTH} znaků.");

            var existingNumbers = new System.Collections.Generic.HashSet<string>(
                _database.SelectFrom<IPurchaseOrderShipmentInfo>()
                    .Where(info => info.PurchaseOrderId == order.Id)
                    .Execute()
                    .Where(info => string.Equals(info.ShipmentProviderSymbol, provider.Symbol, StringComparison.OrdinalIgnoreCase))
                    .Select(info => info.ExternalTrackingNumber)
                    .Where(number => !string.IsNullOrWhiteSpace(number))
                    .Select(number => number.Trim()),
                StringComparer.Ordinal);

            var registeredAt = DateTime.Now;
            foreach (var externalNumber in externalNumbers.Where(number => !existingNumbers.Contains(number)))
            {
                var record = _database.New<IPurchaseOrderShipmentInfo>();
                record.PurchaseOrderId = order.Id;
                record.ShipmentProviderSymbol = provider.Symbol;
                record.ExternalTrackingNumber = externalNumber;
                record.ParcelRegistered = registeredAt;
                _database.Save(record);
            }

            processingLogMessageWriter($"Získána čísla zásilek {provider.Name}: {string.Join(", ", externalNumbers)}");
            return true;
        }
    }
}
