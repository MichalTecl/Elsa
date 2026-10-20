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
        private const string GREETING_PARAM = "greeting";
        private const string LETTER_CASE_PARAM = "letterCase";

        private const string UPPER_CASE = "Velká písmena";

        public List<OrderAggregatorParamModel> Parameters { get; set; } = new List<OrderAggregatorParamModel>
        {
            new OrderAggregatorParamModel
            {
                OptionId = GREETING_PARAM,
                Title = "Pozdrav",
                Values = new List<string> { "Ahoj", "Nazdar", "Čau" }
            },
            new OrderAggregatorParamModel
            {
                OptionId = LETTER_CASE_PARAM,
                Title = "Velikost písmen",
                Values = new List<string> { "Beze změny", UPPER_CASE }
            }
        };

        public AggregatedResultModel Process(IQueryBuilder<IPurchaseOrder> sourceOrdersQuery, Dictionary<string, string> selectedParameters)
        {
            var greeting = selectedParameters[GREETING_PARAM];
            var upperCase = selectedParameters[LETTER_CASE_PARAM] == UPPER_CASE;

            string Format(string value) => upperCase ? value.ToUpperInvariant() : value;

            var model = new AggregatedResultModel();

            model.DefineColumns(new[] { "Column 1", "Column 2" });
            model.DefineRows(new[] { "CZ", "EN" });
            model.RowsTitle = "Jazyk";

            model.Write("EN", "Column 1", Format("Hello"));
            model.Write("EN", "Column 2", Format("World"));
            model.Write("CZ", "Column 1", Format(greeting));
            model.Write("CZ", "Column 2", Format("Světe"));

            model.Legend = "Tahle agregace nedělá nic, je to placeholder pro další vývoj.";

            return model;
        }
    }
}
