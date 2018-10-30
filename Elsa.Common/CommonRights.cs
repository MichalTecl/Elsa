using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.UserRightsInfrastructure;

namespace Elsa.Common
{
    [UserRightsDefinition("Správa uživatelů")]
    public static class CommonRights
    {
        public static readonly UserRight CreateUser = new UserRight("Zakládání nových uživatelů");

        public static readonly UserRight DeactivateUser = new UserRight("Deaktivace uživatelských účtů");

    }
}
