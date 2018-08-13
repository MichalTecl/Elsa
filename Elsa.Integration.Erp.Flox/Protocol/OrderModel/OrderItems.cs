using System.Collections.Generic;
using System.Xml.Serialization;

namespace Elsa.Integration.Erp.Flox.Protocol.OrderModel
{
    public class OrderItems
    {
        [XmlElement("item")]
        public List<OrderItem> Items { get; set; }
    }
}