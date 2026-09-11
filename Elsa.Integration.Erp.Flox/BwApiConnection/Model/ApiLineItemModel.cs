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
            _source = source ?? throw new InvalidOperationException("Seznam položek objednávky obsahuje prázdný záznam.");

            var identification = _source.id ?? _source.item_label ?? "bez ID a názvu";

            if (_source.product == null)
                throw new InvalidOperationException($"Položce objednávky {identification} chybí produkt (product).");

            if (_source.sum_with_tax == null)
                throw new InvalidOperationException($"Položce objednávky {identification} chybí cena s DPH (sum_with_tax).");

            if (_source.price == null)
                throw new InvalidOperationException($"Položce objednávky {identification} chybí cena bez DPH (price).");

            if (_source.weight == null)
                throw new InvalidOperationException($"Položce objednávky {identification} chybí hmotnost (weight).");
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
