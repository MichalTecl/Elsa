using System.Collections.Generic;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderQueryResultModel
    {
        public List<OrderInfoModel> Orders { get; set; }

        public int TotalCount { get; set; }

        public decimal TotalPriceWithVat { get; set; }

        public bool IsAggregated { get; set; }

        public AggregatedResultModel AggregatedResult { get; set; }

        public List<OrderAggregatorParamModel> AggregationParameters { get; set; }

        // <OptionId, SelectedValue>
        public Dictionary<string, string> SelectedAggregationParameters { get; set; }
    }
}
