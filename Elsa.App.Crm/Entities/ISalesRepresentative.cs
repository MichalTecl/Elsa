using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface ISalesRepresentative : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(300, true)]
        string NameInErp { get; set; }

        [NVarchar(300, true)]
        string PublicName { get; set; }

        int? UserId { get; set; }
        IUser User { get; }
    }
}
