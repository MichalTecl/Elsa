using System;

using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface ISysConfig
    {
        int Id { get; }

        int? ProjectId { get; set; }

        IProject Project { get; }

        int? UserId { get; set; }

        IUser User { get;  }

        [NVarchar(256, false)]
        string Key { get; set; }

        [NVarchar(0, false)]
        string ValueJson { get; set; }

        DateTime ValidFrom { get; set; }

        DateTime? ValidTo { get; set; }

        int InsertUserId { get; set; }

        IUser InsertUser { get; }

        bool? ClientSideVisible { get; set; }
    }
}
