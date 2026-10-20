using Elsa.App.OrdersInfo.Model;
using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Elsa.App.OrdersInfo.App.Aggregations
{
    public class OrdersAggregatorFactory
    {
        private readonly IServiceLocator _serviceLocator;

        public OrdersAggregatorFactory(IServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
        }

        public static readonly OrderAggregationTypeModel[] AllTypes = new [] { new OrderAggregationTypeModel(typeof(HelloWorldAggregation), "Testovací") };

        public OrderAggregationTypeModel FindType(string id)
        {
            return AllTypes.FirstOrDefault(x => x.Id == id);
        }

        public IOrdersAggregator Get(string id)
        {
            var t = FindType(id);

            if (t == null)
                return null;

            return _serviceLocator.InstantiateNow<IOrdersAggregator>(t.Type);
        }
    }
}
