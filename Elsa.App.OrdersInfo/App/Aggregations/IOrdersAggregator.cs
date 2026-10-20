using Elsa.App.OrdersInfo.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.OrdersInfo.App.Aggregations
{
    public interface IOrdersAggregator
    {
        List<OrderAggregatorParamModel> Parameters { get; set; }

        AggregatedResultModel Process(AggregationSourceOrders sourceOrders, Dictionary<string, string> selectedParameters);
    }
}
