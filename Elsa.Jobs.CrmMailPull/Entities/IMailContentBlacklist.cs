using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.CrmMailPull.Entities
{
    [Entity]
    public interface IMailContentBlacklist : IIntIdEntity
    {
        [NVarchar(1000, true)]
        string SubjectPattern { get; set; }

        [NVarchar(1000, true)]
        string BodyPattern { get; set; }
    }
}
