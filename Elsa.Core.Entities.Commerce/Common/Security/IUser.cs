using System;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common.Security
{
    [Entity]
    public interface IUser
    {
        int Id { get; }

        int? ParentId { get; set; }

        IUser Parent { get; }

        [NVarchar(255, false)]
        string EMail { get; set; }

        [NVarchar(255, false)]
        string PasswordHash { get; set; }

        [NVarchar(255, false)]
        string Salt { get; set; }

        bool UsesDefaultPassword { get; set; }

        [NVarchar(64, false)]
        string VerificationCode { get; set; }

        DateTime? Verified { get; set; }

        int ProjectId { get; set; }

        IProject Project { get; }
    }
}
