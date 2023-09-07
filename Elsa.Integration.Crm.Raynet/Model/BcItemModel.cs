using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.Crm.Raynet.Model
{
    public class BcItemModel
    {
        public string ProductCode { get; set; }

        // public string Name { get; set; }

        public decimal Price { get; set; }

        // public decimal TaxRate { get; set; }

        public decimal Count { get; set; }

        public decimal DiscountPercent { get; set; }

        public decimal Cost { get; set; }

        // public string Unit { get; set; } = "ks";
    }
}
