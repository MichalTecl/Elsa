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
    public interface ICustomerTagType : IIntIdEntity, IProjectRelatedEntity, IHasAuthor
    {
        bool ForAuthorOnly { get; set; }

        [NVarchar(100, false)]
        string Name { get; set; }

        [NVarchar(1000, false)]
        string Description { get; set; }

        int Priority { get; set; }

        bool CanBeAssignedManually { get; set; }

        [NVarchar(100, true)]
        string OptionGroup {  get; set; }        
    }
}
