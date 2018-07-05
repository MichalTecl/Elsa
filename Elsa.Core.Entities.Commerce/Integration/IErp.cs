﻿using Elsa.Core.Entities.Commerce.Common;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Integration
{
    [Entity]
    public interface IErp
    {
        int Id { get; }

        int ProjectId { get; set; }

        IProject Project { get; }

        [NVarchar(256, false)]
        string Description { get; set; }

        [NVarchar(512, false)]
        string ClientClass { get; set; }

        [NVarchar(-1, true)]
        string ClientData { get; set; }
    }
}
