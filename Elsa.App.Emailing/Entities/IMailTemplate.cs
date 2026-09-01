using System;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.App.Emailing.Entities
{
    [Entity]
    public interface IMailTemplate : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(256, false)]
        string TypeName { get; set; }

        [NVarchar(1000, false)]
        string Subject { get; set; }

        [NVarchar(NVarchar.Max, false)]
        string Body { get; set; }

        DateTime LastChangeDt { get; set; }

        int LastChangeUserId { get; set; }

        IUser LastChangeUser { get; }
    }
}
