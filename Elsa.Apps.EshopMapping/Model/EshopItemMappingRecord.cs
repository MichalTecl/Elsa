using Elsa.Core.Entities.Commerce.Inventory.Kits;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Apps.EshopMapping.Model
{
    public class EshopItemMappingRecord
    {
        public string ElsaMaterialName { get; set; }

        public List<MappedProduct> Products { get; } = new List<MappedProduct>();        
    }

    public class MappedProduct
    {
        public MappedProduct(string erpIconUrl)
        {
            ErpIconUrl = erpIconUrl;
        }

        public bool ErpProductExists { get; set; }
        public string ProductName { get; set; }
        public ProductOrderingInfo OrderingInfo { get; set; }
        public bool SeemsAbandoned { get; set; }
        public List<string> OwningKits { get; } = new List<string>();
        public string ErpIconUrl { get; }

        public IKitDefinition KitDefinition { get; set; }
    }
}
