using System.Xml.Serialization;

using Elsa.Commerce.Core.Model;

namespace Elsa.Integration.Erp.Flox.Protocol.OrderModel
{
    public class OrderItem : IErpOrderItemModel
    {
        private string m_productName = string.Empty;

        [XmlElement("id")]
        public string ErpOrderItemId { get; set; }

        [XmlElement("prod_name")]
        public string ProductName
        {
            get { return m_productName?.Trim(); }
            set { m_productName = value; }
        }

        [XmlElement("quantity")]
        public int Quantity { get; set; }

        [XmlElement("product_id")]
        public string ErpProductId { get; set; }

        [XmlElement("taxed_sum")]
        public string TaxedPrice { get; set; }

        [XmlElement("price")]
        public string PriceWithoutTax { get; set; }

        [XmlElement("tax")]
        public string TaxPercent { get; set; }
    }
}
