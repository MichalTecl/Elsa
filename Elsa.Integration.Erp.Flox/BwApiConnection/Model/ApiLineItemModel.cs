using BwApiClient.Model.Data;
using Elsa.Commerce.Core.Model;
using System;

namespace Elsa.Integration.Erp.Flox.BwApiConnection.Model
{
    public class ApiLineItemModel : IErpOrderItemModel
    {
        private readonly OrderItem _source;

        public ApiLineItemModel(OrderItem source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));            
        }

        public string ErpOrderItemId => _source.id?.Trim();

        public string ProductName => _source.item_label?.Trim();

        public int Quantity => _source.quantity;

        public string ErpProductId => _source.product.id?.Trim();

        public string TaxedPrice => _source.sum_with_tax.value.ToString();

        public string PriceWithoutTax => _source.price.value.ToString();

        public decimal TaxPercent => (decimal)_source.tax_rate;

        public string ProductItemWeight => _source.weight.value.ToString();

        public string ErpWarehouseItemCode => _source.warehouse_number?.Trim();

        public string ErpWarehouseItemId => _source.warehouse_number?.Trim();
    }
}
