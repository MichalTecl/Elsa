using Elsa.App.OrdersInfo.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.OrdersInfo.App.Aggregations
{
    public interface IOrdersAggregator
    {
        AggregatedResultModel Process(IQueryBuilder<IPurchaseOrder> sourceOrdersQuery);
    }
}
