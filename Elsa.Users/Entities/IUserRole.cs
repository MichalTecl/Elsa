using System.Collections.Generic;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Users.Entities
{
    [Entity]
    public interface IUserRole : IIntIdEntity, IProjectRelatedEntity
    {
        [NVarchar(1000, false)]
        string Name { get; set; }

        int? ParentRoleId { get; set; }
        IUserRole ParentRole { get; }

        IEnumerable<IUserRoleRight> AssignedRights { get; }
    }
}
