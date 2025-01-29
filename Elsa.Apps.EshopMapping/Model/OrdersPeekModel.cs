using Elsa.Common.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Elsa.Apps.EshopMapping.Model
{
    public class OrdersPeekModel
    {
        public DateTime BuyDate { get; set; }        
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public bool InKit { get; set; }
        public string OrderDt => StringUtil.FormatDate(BuyDate);
    }

}
