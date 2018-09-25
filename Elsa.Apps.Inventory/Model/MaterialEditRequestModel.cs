using System.Collections.Generic;


namespace Elsa.Apps.Inventory.Model
{
    public class MaterialEditRequestModel
    {
        public int? MaterialId { get; set; }

        public string MaterialName { get; set; }

        public string NominalAmountText { get; set; }

        public List<VirtualProductEditRequestModel.VpMaterialEditRequestModel> Materials { get; set; }
    }
}
