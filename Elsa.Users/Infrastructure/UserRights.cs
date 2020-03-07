using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Elsa.Common.Interfaces;

namespace Elsa.Users.Infrastructure
{
    public static class UserRights
    {
        private static readonly ReaderWriterLockSlim s_lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly IList<UserRight> s_rights = new List<UserRight>();

        public static IEnumerable<UserRight> All
        {
            get
            {
                try
                {
                    s_lock.EnterReadLock();

                    return s_rights.ToArray();
                }
                finally
                {
                    s_lock.ExitReadLock();
                }
            }
        }

        internal static void RegisterType(Type t)
        {
            try
            {
                s_lock.EnterWriteLock();

                foreach (var field in t.GetFields(BindingFlags.Static | BindingFlags.Public)
                    .Where(fi => fi.FieldType == typeof(UserRight)))
                {
                    var right = field.GetValue(null) as UserRight;
                    if (right == null)
                    {
                        continue;
                    }

                    s_rights.Add(right);
                }
            }
            finally
            {
                s_lock.ExitWriteLock();
            }
        }
    }
}
