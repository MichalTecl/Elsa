using System;
using System.Collections.Generic;
using System.IO;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Commerce.Core.Shipment
{
    public interface IShipmentProvider
    {
        byte[] GenerateShipmentRequestDocument(IEnumerable<IPurchaseOrder> orders, bool uniFormat = false);

        string GetOrderNumberByPackageNumber(string packageNumber);

        void SetShipmentMethodsMapping(Dictionary<string, string> mapping);

        Dictionary<string, string> GetShipmentMethodsMapping();
        IEnumerable<string> GetShipmentMethodsList();
    }
}
