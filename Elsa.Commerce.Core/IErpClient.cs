﻿using System;
using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.Erp.Flox;

namespace Elsa.Commerce.Core
{
    public interface IErpClient
    {
        IErp Erp { get; set; }

        IErpDataMapper Mapper { get; }

        IErpCommonSettings CommonSettings { get; }

        IEnumerable<IErpOrderModel> LoadOrders(DateTime from, DateTime? to = null);
        
        void MarkOrderPaid(string orderNumber);

        IErpOrderModel LoadOrder(string orderNumber);
    }
}
