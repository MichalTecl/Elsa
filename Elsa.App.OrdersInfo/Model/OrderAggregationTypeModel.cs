using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.OrdersInfo.Model
{
    public class OrderAggregationTypeModel
    {
        public OrderAggregationTypeModel(Type type, string name)
        {
            Id = type.FullName;
            Name = name;
            Type = type;
        }

        public string Id { get; }
        public string Name { get; }
        public Type Type { get; }
    }
}
