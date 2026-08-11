using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.OrdersInfo.Model
{
    public class SysInfoModel
    {
        public long OrderId { get; set; }

        public List<OrderEventModel> Events { get; set; } = new List<OrderEventModel>();
    }
}
