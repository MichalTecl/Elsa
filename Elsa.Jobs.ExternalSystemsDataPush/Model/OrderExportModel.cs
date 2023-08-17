using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.ExternalSystemsDataPush.Model
{
    public class OrderExportModel
    {
        public long OrderId { get; set; }

        public string OrderNr { get; set; }

        public int OrderStatusId { get; set; }

        public string CustomerRayNetId { get; set; }

        public decimal OrderPrice { get; set; }
        
        public decimal OrderMoc { get; set; }

        public decimal OrderVoc { get; set; }

        public List<OrderItemExportModel> Items { get; } = new List<OrderItemExportModel>(20);
    }

    public class OrderItemExportModel 
    {
        public long OrderId { get; set; }
        public string ProductUid { get; set; }

        public string ProductName { get; set; }

        public decimal ItemQuantity { get; set; }

        public decimal ItemTaxedPrice { get; set; }

        public decimal ItemTaxPercent { get; set; }
    }
}
