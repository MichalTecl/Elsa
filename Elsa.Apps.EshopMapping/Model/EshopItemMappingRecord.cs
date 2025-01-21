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
        public bool ErpProductExists { get; set; }
        public string ProductName { get; set; }
        public int OrderCount { get; set; }
        public string LastOrderedAt { get; set; }
        public bool SeemsAbandoned { get; set; }
    }
}
