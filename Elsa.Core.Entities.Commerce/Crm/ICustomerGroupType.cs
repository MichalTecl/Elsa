using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface ICustomerGroupType : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(300, false)]
        string ErpGroupName { get; set; }

        bool IsDistributor { get; set; }

        bool IsDisabled { get; set; }

        bool? RequiresSalesRep { get; set; }

        [NVarchar(100, true)]
        string DefaultPaymentMethod { get; set; }
    }
}
