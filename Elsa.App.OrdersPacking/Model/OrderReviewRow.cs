using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.OrdersPacking.Model
{
    public class OrderReviewRow
    {
        public long OrderId { get; set; }
        public string OrderNr { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string OrderDt { get; set; }
        public string Shipping { get; set; }
        public string Status { get; set; }
        public string CustomerNote { get; set; }
        public string Price { get; set; }
    }
}
