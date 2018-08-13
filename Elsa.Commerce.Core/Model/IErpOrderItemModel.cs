using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model
{
    public interface IErpOrderItemModel
    {
        string ErpOrderItemId { get; }

        string ProductName { get; }

        int Quantity { get; }

        string ErpProductId { get; }

        string TaxedPrice { get; }

        string PriceWithoutTax { get; }

        string TaxPercent { get; }


    }
}
