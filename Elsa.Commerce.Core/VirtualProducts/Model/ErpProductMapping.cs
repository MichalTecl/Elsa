using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class ErpProductMapping
    {
        [XlsColumn("A", "E-Shop", "@")]
        public string EshopItem { get; set; }

        [XlsColumn("B", "Elsa", "@")]
        public string Material { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is ErpProductMapping eobj))
                return false;

            return (eobj.EshopItem ?? string.Empty).Equals(EshopItem ?? string.Empty)
                && (eobj.Material ?? string.Empty).Equals(Material ?? string.Empty);
        }

        public override int GetHashCode()
        {
            return $"{EshopItem}:{Material}".GetHashCode();
        }
    }
}
