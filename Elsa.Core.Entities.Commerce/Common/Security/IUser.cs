﻿using System;

using Elsa.Core.Entities.Commerce.Core;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common.Security
{
    [Entity]
    public interface IUser : IProjectRelatedEntity
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
        DateTime? LockDt { get; set; }

        [JsonIgnore]
        int? LockUserId { get; set; }

        [JsonIgnore]
        IUser LockUser { get; }
    }
}
