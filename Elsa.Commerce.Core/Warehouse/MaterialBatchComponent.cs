using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Commerce.Core.Warehouse
{
    public class MaterialBatchComponent
    {
        public MaterialBatchComponent(IMaterialBatch batch)
        {
            Batch = batch;
        }

        public decimal ComponentAmount { get; set; }

        public IMaterialUnit ComponentUnit { get; set; }

        public IMaterialBatch Batch { get; }

        public List<MaterialBatchComponent> Components { get; } = new List<MaterialBatchComponent>();
    }
}
