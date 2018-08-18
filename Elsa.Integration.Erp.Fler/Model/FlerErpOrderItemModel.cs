using Elsa.Commerce.Core.Model;

namespace Elsa.Integration.Erp.Fler.Model
{
    public class FlerErpOrderItemModel : IErpOrderItemModel
    {
        private readonly Item m_item;

        public FlerErpOrderItemModel(Item item)
        {
            m_item = item;
        }

        public string ErpOrderItemId => m_item.id_item.ToString();

        public string ProductName => m_item.product_name;

        public int Quantity => 1; //?? wtf

        public string ErpProductId => m_item.id_product.ToString();

        public string TaxedPrice => m_item.price;

        public string PriceWithoutTax => m_item.price;

        public string TaxPercent => string.Empty;
    }
}
