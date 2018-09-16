using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IUnitConversion : IProjectRelatedEntity
    {
        int Id { get; }

        int SourceUnitId { get; set; }
        IMaterialUnit SourceUnit { get; }

        int TargetUnitId { get; set; }
        IMaterialUnit TargetUnit { get; }

        decimal? Multiplier { get; set; }

        [NVarchar(300, true)]
        string ConvertorClass { get; set; }
    }
}
