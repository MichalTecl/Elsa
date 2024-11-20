using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IOrdersSyncHistory : IIntIdEntity
    {
        int ErpId { get; set; }
        IErp Erp { get; }
        DateTime StartDt { get; set; }
        DateTime? EndDt { get; set; }
    }
}
