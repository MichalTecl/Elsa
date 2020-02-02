using System;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;

namespace Elsa.Users.Entities
{
    [Entity]
    public interface IUserRoleMember : IIntIdEntity, IProjectRelatedEntity
    {
        int RoleId { get; set; }
        IUserRole Role { get; }

        int MemberId { get; set; }
        IUser Member { get; }

        int? IncludedById { get; set; }
        IUser IncludedBy { get; }

        DateTime ValidFrom { get; set; }
    }
}
