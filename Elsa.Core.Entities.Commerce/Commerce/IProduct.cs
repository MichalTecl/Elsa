using System;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IProduct : IProjectRelatedEntity
    {
        int Id { get; }
        
        [NVarchar(255, false)]
        string Name { get; set; }

        int? ErpId { get; set; }

        IErp Erp { get; }
        
        [NotFk]
        [NVarchar(255, true)]
        string ErpProductId { get; set; }

        DateTime ProductNameReceivedAt { get; set; }
    }
}
