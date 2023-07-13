using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;

namespace Elsa.Core.Entities.Commerce.Crm
{
    [Entity]
    public interface ICustomerEmailChange : IProjectRelatedEntity, IIntIdEntity
    {
        [NVarchar(255, true)]
        string OldEmail { get; set; }

        [NVarchar(255, true)]
        string NewEmail { get; set; }

        DateTime ChangeDt { get; set; }

        [NVarchar(100, true)]
        string ErpUid { get; set; }
    }
}
