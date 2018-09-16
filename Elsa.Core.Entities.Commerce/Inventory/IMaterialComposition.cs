using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IMaterialComposition : IMaterialCompositionBase
    {
        int Id { get; }

        int CompositionId { get; set; }
        IMaterial Composition { get; }
    }
}
