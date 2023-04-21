using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

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
        private readonly IOrdersFacade m_ordersFacade;
        private readonly IShipmentProvider m_shipmentProvider;

        public ShipmentController(IWebSession webSession, ILog log, IOrdersFacade ordersFacade, IShipmentProvider shipmentProvider)
            : base(webSession, log)
        {
            m_ordersFacade = ordersFacade;
            m_shipmentProvider = shipmentProvider;
        }

        public FileResult GetShipmentRequestDocument(bool uniFormat, string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentNullException(nameof(provider));

            var orders = m_ordersFacade.GetAndSyncPaidOrders(DateTime.Now.AddDays(-30), provider).ToList();

            var data = m_shipmentProvider.GenerateShipmentRequestDocument(orders, uniFormat);

            var xname = uniFormat ? "DPD" : "zasilkovna";

            return new FileResult($"{xname}_{DateTime.Now:ddMMyyyy}.csv", data);
        }

        public HtmlResult GetShipmentMethodNamesList() 
        {
            var methods = m_shipmentProvider.GetShipmentMethodsList();
            var sb = new StringBuilder();
                        
            foreach (var sm in methods.OrderBy(m => m).Distinct())
                sb.Append($"{sm}<br>");

            return new HtmlResult(sb.ToString());
        }

        public MappingDocModel GetShipmentMapping()
        {
            var mapping = m_shipmentProvider.GetShipmentMethodsMapping();

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

            m_shipmentProvider.SetShipmentMethodsMapping(map);

            return GetShipmentMapping();
        }

        public class MappingDocModel
        {
            public string Mapping { get; set; }
        }
    }
}
