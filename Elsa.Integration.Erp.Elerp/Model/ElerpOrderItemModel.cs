using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;

namespace Elsa.Integration.Erp.Elerp.Model
{
    internal class ElerpOrderItemModel : IErpOrderItemModel
    {
        public string ErpOrderItemId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public string ErpProductId { get; set; }

        public string TaxedPrice { get; set; }

        public string PriceWithoutTax { get; set; }

        public string TaxPercent { get; set; }
        public string ProductItemWeight { get; set; }
    }
}
