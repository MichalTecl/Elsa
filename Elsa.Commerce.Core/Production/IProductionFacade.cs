﻿
using System.Collections.Generic;

using Elsa.Commerce.Core.Production.Model;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.Production
{
    public interface IProductionFacade
    {
        IEnumerable<IMaterialBatch> LoadProductionBatches(long? fromDt, int pageSize);
    }
}
