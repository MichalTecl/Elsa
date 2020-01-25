using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting
{
    [Entity]
    public interface IFixedCostValue : IIntIdEntity, IProjectRelatedEntity
    {
        int FixedCostTypeId { get; set; }
        IFixedCostType FixedCostType { get; }

        int Year { get; set; }

        int Month { get; set; }

        decimal Value { get; set; }
    }
}
