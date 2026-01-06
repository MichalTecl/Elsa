using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Elsa.App.OrdersPacking;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Shipment;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.App.Shipment
{
    [Controller("shipment")]
    public class ShipmentController : ElsaControllerBase
    {
        private readonly IOrdersFacade _ordersFacade;
        private readonly IShipmentProvider _shipmentProvider;
        private readonly ShipmentRequestGeneratorFactory _shipmentRequestGeneratorFactory;

        public ShipmentController(IWebSession webSession, ILog log, IOrdersFacade ordersFacade, IShipmentProvider shipmentProvider, ShipmentRequestGeneratorFactory shipmentRequestGeneratorFactory)
            : base(webSession, log)
        {
            _ordersFacade = ordersFacade;
            _shipmentProvider = shipmentProvider;
            _shipmentRequestGeneratorFactory = shipmentRequestGeneratorFactory;
        }

        public FileResult GetShipmentRequestDocument(bool uniFormat, string provider)
        {
            EnsureUserRight(OrdersPackingUserRights.DownloadTrackingDocument);

            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentNullException(nameof(provider));

            var generator = _shipmentRequestGeneratorFactory.Get(provider);

            var orders = _ordersFacade.GetAndSyncPaidOrders(provider).ToList();

            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            {
                generator.Generate(orders, streamWriter, out var fileName);

                streamWriter.Flush();

                return new FileResult(fileName, stream.ToArray());
            }           
        }

        public HtmlResult GetShipmentMethodNamesList() 
        {
            var methods = _shipmentProvider.GetShipmentMethodsList();
            var sb = new StringBuilder();
                        
            foreach (var sm in methods.OrderBy(m => m).Distinct())
                sb.Append($"{sm}<br>");

            return new HtmlResult(sb.ToString());
        }

        public MappingDocModel GetShipmentMapping()
        {
            var mapping = _shipmentProvider.GetShipmentMethodsMapping();

            var sb = new StringBuilder();
            foreach (var map in mapping)
            {
                sb.AppendLine($"{map.Key} : {map.Value}");
            }

            return new MappingDocModel {Mapping = sb.ToString()};
        }

        public MappingDocModel SetShipmentMapping(MappingDocModel mapping)
        {
            var lines = mapping.Mapping.Split(new char[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);

            var map = new Dictionary<string,string>(lines.Length);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split(':').Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray();
                if (parts.Length != 2)
                {
                    throw new InvalidOperationException($"Neplatné pravidlo \"{line}\" Pravidlo musí být ve tvaru \"název_v_eshopu : název_v_zásilkovně\"");
                }

                if (map.ContainsKey(parts[0]))
                {
                    throw new InvalidOperationException($"Více než jedno pravidlo pro název v eshopu \"{parts[0]}\"");
                }

                map[parts[0]] = parts[1];
            }

            _shipmentProvider.SetShipmentMethodsMapping(map);

            return GetShipmentMapping();
        }

        public class MappingDocModel
        {
            public string Mapping { get; set; }
        }
    }
}
