using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Apps.EshopMapping.Model
{
    internal class ProductOrderingInfo
    {
        public string PlacedName { get; set; }
        public int OrderCount { get; set; }
        public DateTime LastOrder { get; set; }
    }
}
