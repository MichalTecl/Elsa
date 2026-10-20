using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderAggregatorParamModel
    {
        public string OptionId { get; set; }

        public string Title { get; set; }

        public List<string> Values { get; set; }
    }
}
