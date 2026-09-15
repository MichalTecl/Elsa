using System.Collections.Generic;

namespace Elsa.Commerce.Core.Shipment
{
    public class ShipmentFacade : IShipmentFacade
    {
        private readonly ILegacyShipmentService _legacyShipmentService;

        public ShipmentFacade(ILegacyShipmentService legacyShipmentService)
        {
            _legacyShipmentService = legacyShipmentService;
        }

        public string GetOrderNumberByPackageNumber(string packageNumber)
        {
            return _legacyShipmentService.GetOrderNumberByPackageNumber(packageNumber);
        }

        public void SetShipmentMethodsMapping(Dictionary<string, string> mapping)
        {
            _legacyShipmentService.SetShipmentMethodsMapping(mapping);
        }

        public Dictionary<string, string> GetShipmentMethodsMapping()
        {
            return _legacyShipmentService.GetShipmentMethodsMapping();
        }

        public IEnumerable<string> GetShipmentMethodsList()
        {
            return _legacyShipmentService.GetShipmentMethodsList();
        }
    }
}
