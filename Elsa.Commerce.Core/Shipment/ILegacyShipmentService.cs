using System.Collections.Generic;

namespace Elsa.Commerce.Core.Shipment
{
    /// <summary>
    /// Bridge to the existing shipment implementation used by ShipmentFacade.
    /// New carrier-specific operations belong in separate provider contracts.
    /// </summary>
    public interface ILegacyShipmentService
    {
        string GetOrderNumberByPackageNumber(string packageNumber);
        void SetShipmentMethodsMapping(Dictionary<string, string> mapping);
        Dictionary<string, string> GetShipmentMethodsMapping();
        IEnumerable<string> GetShipmentMethodsList();
    }
}
