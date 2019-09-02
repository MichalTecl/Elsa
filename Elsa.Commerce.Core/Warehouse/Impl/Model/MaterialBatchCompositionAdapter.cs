using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire;

namespace Elsa.Commerce.Core.Warehouse.Impl.Model
{
    internal class MaterialBatchCompositionAdapter : AdapterBase<IMaterialBatchComposition>, IMaterialBatchComposition
    {
        private readonly IMaterialBatchComposition m_adaptee;
        public MaterialBatchCompositionAdapter(IServiceLocator serviceLocator, IMaterialBatchComposition adaptee) : base(serviceLocator, adaptee)
        {
            m_adaptee = adaptee;
        }

        public int Id => m_adaptee.Id;
        public int CompositionId { get => m_adaptee.CompositionId; set => m_adaptee.CompositionId = value; }
        public int ComponentId { get => m_adaptee.ComponentId; set => m_adaptee.ComponentId = value; }
        public int UnitId { get => m_adaptee.UnitId; set => m_adaptee.UnitId = value; }

        public IMaterialBatch Composition =>
            Get<IMaterialBatchRepository, IMaterialBatch>("Composition", r => r.GetBatchById(CompositionId)?.Batch);
        
        public IMaterialBatch Component => Get<IMaterialBatchRepository, IMaterialBatch>("Component", r => r.GetBatchById(ComponentId)?.Batch);

        public IMaterialUnit Unit
        {
            get
            {
                return Get<IUnitRepository, IMaterialUnit>("Unit", r => r.GetUnit(UnitId));
            }
        }

        public decimal Volume { get => m_adaptee.Volume; set => m_adaptee.Volume = value; }
    }
}
