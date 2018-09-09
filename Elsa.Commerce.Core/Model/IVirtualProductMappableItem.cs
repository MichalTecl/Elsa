namespace Elsa.Commerce.Core.Model
{
    public interface IVirtualProductMappableItem
    {
        int? ErpId { get; }

        string ErpProductId { get; }

        string ItemName { get; }
    }
}
