using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire.RobOrm.Core;

namespace Elsa.Users.Entities
{
    [Entity]
    public interface IUserRoleRight : IIntIdEntity
    {
        int RoleId { get; set; }
        IUserRole Role { get; }

        int RightId { get; set; }
        IUserRight Right { get; }

        int AssignedById { get; set; }
        IUser AssignedBy { get; }

        DateTime AssignDt { get; set; }
    }
}
