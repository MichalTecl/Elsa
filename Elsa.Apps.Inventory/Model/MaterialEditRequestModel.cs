using System.Collections.Generic;


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

        public List<VirtualProductEditRequestModel.VpMaterialEditRequestModel> Materials { get; set; }
    }
}
