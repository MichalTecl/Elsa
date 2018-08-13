using System.Collections.Generic;
using System.Xml.Serialization;

namespace Elsa.Integration.Erp.Flox.Protocol.OrderModel
{
    public class OrdersList
    {
        [XmlElement("order")]
        public List<FloxErpOrderModel> Orders { get; set; }
    }
}
