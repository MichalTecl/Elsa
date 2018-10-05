using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Integration;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory
{
    public interface IMappedToOrderItem
    {
        [JsonIgnore]
        int? ErpId { get; set; }

        [JsonIgnore]
        IErp Erp { get; }

        [JsonIgnore]
        [NotFk]
        [NVarchar(255, true)]
        string ErpProductId { get; set; }

        [NVarchar(512, true)]
        string ItemName { get; set; }
    }
}
