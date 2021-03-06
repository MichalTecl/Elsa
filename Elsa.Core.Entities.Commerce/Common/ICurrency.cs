﻿using Elsa.Core.Entities.Commerce.Core;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface ICurrency : IProjectRelatedEntity
    {
        int Id { get; }

        [NVarchar(8, false)]
        string Symbol { get; set; }

        bool IsProjectMainCurrency { get; set; }
    }
}
