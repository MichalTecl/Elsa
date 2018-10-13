using System;
using System.ComponentModel;

using Newtonsoft.Json;

namespace Elsa.App.Crm.Model
{
    public class CustomerRelatedItem
    {
        public const string MessageItemType = "message";
        public const string OrderItemType = "order";

        public string DisplayTime { get; set; }

        [JsonIgnore]
        public long SortTime { get; set; }

        public string ItemType { get; set; }

        public object Item { get; set; }

        public static CustomerRelatedItem Create(DateTime dt, string itemType, object item)
        {
            return new CustomerRelatedItem()
                       {
                           SortTime = dt.Ticks,
                           DisplayTime = dt.ToString("MM/yyyy"),
                           ItemType = itemType,
                           Item = item
                       };
        }
    }
}
