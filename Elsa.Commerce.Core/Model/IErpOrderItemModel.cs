namespace Elsa.Commerce.Core.Model
{
    public interface IErpOrderItemModel
    {
        string ErpOrderItemId { get; }

        string ProductName { get; }

        int Quantity { get; }

        string ErpProductId { get; }

        string TaxedPrice { get; }

        string PriceWithoutTax { get; }

        decimal TaxPercent { get; }

        string ProductItemWeight { get; }
        string ErpWarehouseItemCode { get; }
        string ErpWarehouseItemId { get; }
    }
}
