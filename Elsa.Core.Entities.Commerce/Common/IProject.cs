using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Common.Security;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface IProject
    {
        int Id { get; }

        [NVarchar(128, false)]
        string Name { get; set; }

        [JsonIgnore]
        IEnumerable<IUser> Users { get; }
    }
}
