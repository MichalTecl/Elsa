using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Core.Entities.Commerce.Common
{
    public interface IVolumeAndUnit
    {
        decimal Volume { get; set; }
        int UnitId { get; set; }
        IMaterialUnit Unit { get; }
    }
}
