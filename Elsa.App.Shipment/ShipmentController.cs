using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Shipment;
using Elsa.Common;
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

        public FileResult GetShipmentRequestDocument()
        {
            var orders = m_ordersFacade.GetAndSyncPaidOrders(DateTime.Now.AddDays(-30)).ToList();

            var data = m_shipmentProvider.GenerateShipmentRequestDocument(orders);
            return new FileResult($"zasilkovna_{DateTime.Now:ddMMyyyy}.csv", data);
        }
    }
}
