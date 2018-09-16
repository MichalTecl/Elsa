namespace Elsa.Core.Entities.Commerce.Inventory
{
    public interface IMaterialCompositionBase
    {
        int ComponentId { get; set; }
        IMaterial Component { get; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        decimal Amount { get; set; }
    }
}
