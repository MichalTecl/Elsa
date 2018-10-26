using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.UserRightsInfrastructure
{
    internal sealed class UserRightInfo
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
    }
}
