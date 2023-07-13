using System.Xml.Serialization;

using Elsa.Commerce.Core.Model;

namespace Elsa.Integration.Erp.Flox.Protocol.OrderModel
{
    public class ErpPriceElement : IErpPriceElementModel
    {
        [XmlElement("id")]
        public string ErpPriceElementId { get; set; }

        [XmlElement("type")]
        public string TypeErpName { get; set; }

        [XmlElement("title")]
        public string Title { get; set; }

        [XmlElement("value")]
        public string Value { get; set; }

        [XmlElement("tax")]
        public string TaxPercent { get; set; }

        [XmlElement("price")]
        public string Price { get; set; }
    }
}
