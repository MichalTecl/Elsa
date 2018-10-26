using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Robowire;

namespace Elsa.Common
{
    public class UserRightsDefinitionAttribute : Attribute, ISelfSetupAttribute
    {
        public void Setup(Type markedType, IContainerSetup setup)
        {
            
        }
    }
}
