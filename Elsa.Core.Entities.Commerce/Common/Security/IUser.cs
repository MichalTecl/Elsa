using System;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common.Security
{
    [Entity]
    public interface IUser
    {
        int Id { get; }

        [JsonIgnore]
        int? ParentId { get; set; }

        [JsonIgnore]
        IUser Parent { get; }

        [NVarchar(255, false)]
        string EMail { get; set; }

        [JsonIgnore]
        [NVarchar(255, false)]
        string PasswordHash { get; set; }

        [JsonIgnore]
        [NVarchar(255, false)]
        string Salt { get; set; }
        
        bool UsesDefaultPassword { get; set; }

        [JsonIgnore]
        [NVarchar(64, false)]
        string VerificationCode { get; set; }

        [JsonIgnore]
        DateTime? Verified { get; set; }

        [JsonIgnore]
        int ProjectId { get; set; }

        [JsonIgnore]
        IProject Project { get; }
    }
}
