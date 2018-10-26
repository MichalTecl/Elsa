using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.UserRightsInfrastructure
{
    public static class UserRightDefinitionCollector
    {
        private static readonly ConcurrentDictionary<string, UserRightInfo> s_rights = new ConcurrentDictionary<string, UserRightInfo>();


    }


}
