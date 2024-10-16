﻿using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.OrdersPacking.Model
{
    public class PackingOrderModel
    {
        public long OrderId { get; set; }

        public string ErpName { get; set; }

        public string OrderNumber { get; set; }

        public string CustomerName { get; set; }

        public string CustomerEmail { get; set; }

        public string Price { get; set; }

        public string InternalNote { get; set; }

        public string CustomerNote { get; set; }

        public string DiscountsText { get; set; }

        public string PackingWarning { get; set; }

        public List<PackingOrderItemModel> Items { get; } = new List<PackingOrderItemModel>();
    }
}
