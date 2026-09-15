using System;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Apps.Reporting.Entities
{
    [Entity]
    public interface IBulletinView : IIntIdEntity, IProjectRelatedEntity
    {
        int UserId { get; set; }
        IUser User { get; }

        [NVarchar(100, false)]
        string BulletinFileName { get; set; }

        DateTime OpenedAt { get; set; }
    }
}