﻿using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core;
using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Entities
{
    [Entity]
    public interface IMeetingStatusAction : IIntIdEntity
    {
        [NVarchar(100, false)]
        string Title { get; set; }
        int CurrentStatusTypeId { get; set; }
        IMeetingStatus CurrentStatusType { get; }
        int NextStatusTypeId { get; set; }
        IMeetingStatus NextStatusType { get; }
        int SortOrder { get; set; }
    }
}
