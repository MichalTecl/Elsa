using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.MaterialLevels.Entities
{
    [Entity]
    public interface IMaterialOrderEvent : IIntIdEntity
    {
        int MaterialId { get; set; }
        IMaterial Material { get; }
        DateTime OrderDt { get; set; }
        DateTime InsertDt { get; set; }
        int UserId { get; set; }
        IUser User { get; }
        DateTime? DeliveryDeadline {  get; set; }
        DateTime? DeadlineSetDt { get; set; }
        int? DeadlineSetUserId { get; set; }
        IUser DeadlineSetUser { get; }
    }
}
