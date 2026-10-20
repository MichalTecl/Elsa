using Elsa.App.OrdersInfo.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.OrdersInfo.App.Aggregations
{
    public class HelloWorldAggregation : IOrdersAggregator
    {
        public AggregatedResultModel Process(IQueryBuilder<IPurchaseOrder> sourceOrdersQuery)
        {
            var model = new AggregatedResultModel();

            model.DefineColumns(new[] { "Column 1", "Column 2" });
            model.DefineRows(new[] { "CZ", "EN" });
            model.RowsTitle = "Jazyk";

            model.Write("EN", "Column 1", "Hello");
            model.Write("EN", "Column 2", "World");
            model.Write("CZ", "Column 1", "Ahoj");
            model.Write("CZ", "Column 2", "Světe");

            model.Legend = "Tahle agregace nedělá nic, je to placeholder pro další vývoj.";

            return model;
        }
    }
}
