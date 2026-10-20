using System.Collections.Generic;

namespace Elsa.App.OrdersInfo.App.Aggregations
{
    /// <summary>
    /// Aggregator able to list orders behind a cell. The cell is identified by a drilldown key
    /// which the aggregator itself assigned by AggregatedResultModel.SetOrdersDrilldown.
    /// </summary>
    public interface IOrdersDrilldownAggregator : IOrdersAggregator
    {
        /// <summary>
        /// Returns Ids of source orders (orders matching the user's filter) belonging to the cell
        /// </summary>
        IList<long> GetDrilldownOrderIds(AggregationSourceOrders sourceOrders, Dictionary<string, string> selectedParameters, string drilldownKey);
    }
}
