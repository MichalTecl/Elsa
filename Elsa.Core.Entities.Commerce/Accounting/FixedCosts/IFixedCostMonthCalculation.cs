using System;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Accounting.FixedCosts
{
    [Entity]
    public interface IFixedCostMonthCalculation : IIntIdEntity, IProjectRelatedEntity
    {
        int Year { get; set; }
        int Month { get; set; }

        decimal ValueToDistribute { get; set; }

        int CalcUserId { get; set; }
        IUser CalcUser { get; }

        DateTime CalcDt { get; set; }

        decimal DistributeToTotal { get; set; }

        decimal DistributionDiv { get; set; }
    }
}
