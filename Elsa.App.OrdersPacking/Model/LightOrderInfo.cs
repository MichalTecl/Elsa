using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.App.OrdersPacking.Model
{
    public class LightOrderInfo
    {
        public LightOrderInfo(IPurchaseOrder po)
        {
            OrderId = po.Id;
            OrderNumber = po.OrderNumber;
            CustomerName = string.IsNullOrWhiteSpace(po.CustomerName)
                               ? $"{po.InvoiceAddress?.FirstName} {po.InvoiceAddress?.LastName}"
                               : po.CustomerName;

            Items = po.Items.Select(i => new LightItemInfo(i)).ToList();
        }

        public long OrderId { get; }

        public string OrderNumber { get; }

        public string CustomerName { get; }
        
        public List<LightItemInfo> Items { get; }

        public class LightItemInfo
        {
            public LightItemInfo(IOrderItem item)
            {
                Quantity = item.Quantity;
                Name = item.PlacedName;
            }

            public decimal Quantity { get; }
            public string Name { get; }
        }
    }
}
