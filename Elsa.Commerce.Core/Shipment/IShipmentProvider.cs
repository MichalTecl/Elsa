﻿using System.Collections.Generic;
using System.IO;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core.Shipment
{
    public interface IShipmentProvider
    {
        byte[] GenerateShipmentRequestDocument(IEnumerable<IPurchaseOrder> orders);

        string GetOrderNumberByPackageNumber(string packageNumber);
    }
}