using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    public interface IMappedToOrderItem
    {
        int? ErpId { get; set; }
        IErp Erp { get; }

        [NotFk]
        [NVarchar(255, true)]
        string ErpProductId { get; set; }

        [NVarchar(512, true)]
        string ItemName { get; set; }
    }
}
