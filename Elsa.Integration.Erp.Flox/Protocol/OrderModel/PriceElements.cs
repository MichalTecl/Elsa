using System.Collections.Generic;
using System.Xml.Serialization;

namespace Elsa.Integration.Erp.Flox.Protocol.OrderModel
{

    public class PriceElements
    {
        [XmlElement("item")]
        public List<ErpPriceElement> Items { get; set; }
    }
}
