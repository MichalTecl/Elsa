using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.UserRightsInfrastructure
{
    public sealed class UserRight
    {
        public UserRight(string description)
        {
            Description = description;
        }
        
        public string Description { get; }
    }
}
