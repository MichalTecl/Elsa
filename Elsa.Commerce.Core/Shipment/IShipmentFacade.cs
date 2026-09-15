using System.Collections.Generic;

namespace Elsa.Commerce.Core.Shipment
{
    public interface IShipmentFacade
    {
        string GetOrderNumberByPackageNumber(string packageNumber);
        void SetShipmentMethodsMapping(Dictionary<string, string> mapping);
        Dictionary<string, string> GetShipmentMethodsMapping();
        IEnumerable<string> GetShipmentMethodsList();
    }
}
