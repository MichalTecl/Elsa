using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Elsa.Common.UserRightsInfrastructure
{
    public static class UserRightDefinitionCollector
    {
        private static readonly ConcurrentDictionary<string, UserRightInfo> s_rights = new ConcurrentDictionary<string, UserRightInfo>();

        public static void RegisterType(Type t)
        {
            foreach (var ur in UserRightInfo.ScanType(t))
            {
                UserRightInfo existing;
                if (s_rights.TryGetValue(ur.Name, out existing))
                {
                    if (ur.DeclaringType != existing.DeclaringType || ur.FullName != existing.FullName)
                    {
                        throw new InvalidOperationException($"There is duplicate UserRight name '{ur.Name}'");
                    }

                    continue;
                }

                s_rights.AddOrUpdate(ur.Name, ur, (s, u) => u);
            }
        }

        public static IEnumerable<UserRightInfo> GetAllUserRights()
        {
            return s_rights.Values;
        }
    }


}
