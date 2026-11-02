using Elsa.Core.Entities.Commerce.Common;
using RobOrm.Core;
using RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMessageParticipantAddress : IIntIdEntity
    {
        [NVarchar(100, false)]
        string Email { get; set; }
    }
}
