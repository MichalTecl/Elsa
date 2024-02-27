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
        private string _eshop = null;
        private string _elsa = null;

        [XlsColumn("A", "E-Shop", "@")]
        public string EshopItem 
        { 
            get { return _eshop?.Trim(); } 
            set { _eshop = value; } 
        }

        [XlsColumn("B", "Elsa", "@")]
        public string Material 
        {
            get { return _elsa?.Trim(); }
            set { _elsa = value; } 
        }

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
