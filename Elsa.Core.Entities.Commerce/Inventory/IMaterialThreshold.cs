using System;

using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    [Entity]
    public interface IMaterialThreshold : IProjectRelatedEntity
    {
        int Id { get; }

        int MaterialId { get; set; }
        IMaterial Material { get; }

        decimal ThresholdQuantity { get; set; }

        int UnitId { get; set; }
        IMaterialUnit Unit { get; }

        DateTime UpdateDt { get; set; }

        int UpdateUserId { get; set; }
        IUser UpdateUser { get; }
    }
}
