namespace Elsa.Commerce.Core.Model
{
    public interface IErpPriceElementModel
    {
        string ErpPriceElementId { get; }

        string TypeErpName { get; }

        string Title { get; }

        string Value { get; }

        string TaxPercent { get; }

        string Price { get; }
    }
}
