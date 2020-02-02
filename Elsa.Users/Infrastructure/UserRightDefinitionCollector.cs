using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elsa.Common.Interfaces;

namespace Elsa.Users.Infrastructure
{
    public static class UserRightDefinitionCollector
    {
        private static readonly ConcurrentDictionary<string, UserRight> s_rights = new ConcurrentDictionary<string, UserRight>();

        public static void RegisterType(Type t)
        {
            
        }

        public static IEnumerable<UserRight> GetAllUserRights()
        {
            yield break;
        }
    }


}
