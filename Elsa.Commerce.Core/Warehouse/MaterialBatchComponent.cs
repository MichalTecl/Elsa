using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire;

namespace Elsa.Commerce.Core.Warehouse
{
    public class MaterialBatchComponent
    {
        private readonly Lazy<List<MaterialBatchComponent>> m_components;
        
        public MaterialBatchComponent(IMaterialBatch batch, IMaterialBatchRepository batchRepository) 
        {
            Batch = batch;
            IsLocked = batch.LockDt != null && batch.LockDt <= DateTime.Now;
            IsClosed = batch.CloseDt != null && batch.CloseDt <= DateTime.Now;

            ComponentUnit = batch.Unit;
            ComponentAmount = batch.Volume;

            m_components = new Lazy<List<MaterialBatchComponent>>(() =>
            {
                var result = new List<MaterialBatchComponent>();
                foreach (var component in batch.Components)
                {
                    var componentModel = batchRepository.GetBatchById(component.ComponentId);
                    componentModel.ComponentAmount = component.Volume;
                    componentModel.ComponentUnit = component.Unit;
                    result.Add(componentModel);
                }

                return result;
            });
        }

        public decimal ComponentAmount { get; set; }

        public IMaterialUnit ComponentUnit { get; set; }

        public IMaterialBatch Batch { get; }

        public bool IsLocked { get; }

        public bool IsClosed { get; }

        public List<MaterialBatchComponent> Components => m_components.Value;
    }
}
