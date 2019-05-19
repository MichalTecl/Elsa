
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common.SystemCounters
{
    [Entity]
    public interface ISystemCounter : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(100, false)]
        string Name { get; set; }

        [NVarchar(100, true)]
        string StaticPrefix { get; set; }

        [NVarchar(100, true)]
        string StaticSuffix { get; set; }

        [NVarchar(100, true)]
        string DtSeparator { get; set; }

        [NVarchar(30, true)]
        string DtFormat { get; set; }

        [NVarchar(30, true)]
        string LastDtValue { get; set; }

        long CounterValue { get; set; }

        long CounterMinValue { get; set; }

        int CounterPadding { get; set; }
    }
}
