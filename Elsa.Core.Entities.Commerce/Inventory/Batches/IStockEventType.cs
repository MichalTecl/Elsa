﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Inventory.Batches
{
    [Entity]
    public interface IStockEventType : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(64, false)]
        string Name { get; set; }

        [NVarchar(64, false)]
        string TabTitle { get; set; }

        bool CanBeMinus { get; set; }

        bool CanBePlus { get; set; }

        bool RequiresNote { get; set; }
    }
}