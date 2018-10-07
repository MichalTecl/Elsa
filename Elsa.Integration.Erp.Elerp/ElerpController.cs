﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Integration.Erp.Flox;

using Robowire.RoboApi;

namespace Elsa.Integration.Erp.Elerp
{
    [Controller("elerp")]
    public class ElerpController : ElsaControllerBase
    {
        private readonly ElerpClient m_elerp;
        private readonly IErpClientFactory m_erpClientFactory;

        public ElerpController(IWebSession webSession, ILog log, ElerpClient elerp, IErpClientFactory erpClientFactory)
            : base(webSession, log)
        {
            m_elerp = elerp;
            m_erpClientFactory = erpClientFactory;
        }

        public void CopyFromFlox(string orderNumber)
        {
            var flox =
                m_erpClientFactory.GetAllErpClients()
                    .First(c => c.Erp.Description.Equals("Flox", StringComparison.InvariantCultureIgnoreCase));

            var order = flox.LoadOrder(orderNumber);

            if (order == null)
            {
                throw new InvalidOperationException("Order not found in source");
            }

            var nnum = $"EL{orderNumber}";

            order.SetDebugNumber(nnum);

            m_elerp.SaveOrder(order);
        }
    }

    
}
