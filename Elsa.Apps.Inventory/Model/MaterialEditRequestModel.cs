using System.Collections.Generic;

using Elsa.Commerce.Core.VirtualProducts.Model;

namespace Elsa.Apps.Inventory.Model
{
    public class MaterialEditRequestModel
    {
        public int? MaterialId { get; set; }

        public string MaterialName { get; set; }

        public string NominalAmountText { get; set; }

        public int MaterialInventoryId { get; set; }

        public bool AutomaticBatches { get; set; }

        public bool RequiresInvoice { get; set; }

        public bool RequiresPrice { get; set; }

        public bool RequiresProductionPrice { get; set; }

        public bool HasThreshold { get; set; }
        public string ThresholdText { get; set; }

        public List<VirtualProductEditRequestModel.VpMaterialEditRequestModel> Materials { get; set; }
        
        public bool RequiresSupplierReference { get; set; }

        public bool Autofinalization { get; set; }
    }
}
