using Newtonsoft.Json;
using RobOrm.Core;
using RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Core.Entities.Commerce.Common.Security
{
    [Entity]
    public interface IUserPasswordHistory : IIntIdEntity
    {
        int UserId { get; set; }
        IUser User { get; }

        DateTime InsertDt { get; set; }

        [JsonIgnore]
        [NVarchar(255, false)]
        string PasswordHash { get; set; }        
    }
}
