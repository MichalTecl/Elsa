using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.SystemCounters;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IReleasingFormsGenerationTask : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(300, false)]
        string FormText { get; set; }

        [NVarchar(300, false)]
        string GeneratorName { get; set; }

        int CounterId { get; set; }
        ISystemCounter Counter { get; }

        IEnumerable<IReleasingFormsGenerationTaskInventory> Inventories { get; }
    }
}
