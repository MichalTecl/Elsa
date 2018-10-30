using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.UserRightsInfrastructure
{
    public sealed class UserRightInfo
    {
        public readonly string Name;
        public readonly string FullName;
        public readonly string Description;
        public readonly Type DeclaringType;

        public UserRightInfo(string name, string fullName, Type declaringType, string description)
        {
            Name = name;
            FullName = fullName;
            DeclaringType = declaringType;
            Description = description;
        }

        public static IEnumerable<UserRightInfo> ScanType(Type t)
        {
            var atr =
                Attribute.GetCustomAttribute(t, typeof(UserRightsDefinitionAttribute)) as UserRightsDefinitionAttribute;

            if (atr == null)
            {
                yield break;
            }

            foreach (
                var field in
                t.GetFields(BindingFlags.Static | BindingFlags.Public).Where(f => f.FieldType == typeof(UserRight)))
            {
                var value = field.GetValue(null) as UserRight;

                if (value == null)
                {
                    yield return new UserRightInfo(field.Name, $"{atr.Path}.{field.Name}", t, string.Empty);
                }
                else
                {
                    yield return new UserRightInfo(field.Name, $"{atr.Path}.{field.Name}", t, value.Description ?? string.Empty);
                }
            }
        }
    }
}
