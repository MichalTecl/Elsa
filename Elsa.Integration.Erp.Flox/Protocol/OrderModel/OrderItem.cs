using Elsa.Commerce.Core.Model;
using System.Xml.Serialization;

namespace Elsa.Integration.Erp.Flox.Protocol.OrderModel
{
    public class OrderItem : IErpOrderItemModel
    {
        private string m_productName = string.Empty;

        [XmlElement("item_id")]
        //[XmlElement("link")]
        public string ErpOrderItemId
        {
            get;
            set;
        }

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
        public string TaxPercentValue { get; set; }

        [XmlElement("weight")]
        public string ProductItemWeight { get; set; }

        public decimal TaxPercent
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TaxPercentValue) || !decimal.TryParse(TaxPercentValue, out var val))
                    return 21;

                return val;
            }
        }

        [XmlElement("warehouse_item")]
        public string ErpWarehouseItemCode { get; set; }

        [XmlElement("warehouse_item_id")]
        public string ErpWarehouseItemId { get; set; }
    }
}
