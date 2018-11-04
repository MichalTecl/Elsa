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
            IsLocked = batch.LockDt != null && batch.LockDt <= DateTime.Now;
            IsClosed = batch.CloseDt != null && batch.CloseDt <= DateTime.Now;

            ComponentUnit = batch.Unit;
            ComponentAmount = batch.Volume;
        }

        public decimal ComponentAmount { get; set; }

        public IMaterialUnit ComponentUnit { get; set; }

        public IMaterialBatch Batch { get; }

        public bool IsLocked { get; }

        public bool IsClosed { get; }

        public List<MaterialBatchComponent> Components { get; } = new List<MaterialBatchComponent>();
    }
}
