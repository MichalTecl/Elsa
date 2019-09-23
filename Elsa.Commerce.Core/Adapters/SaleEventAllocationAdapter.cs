using System;
using Elsa.Commerce.Core.SaleEvents;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire;

namespace Elsa.Commerce.Core.Adapters
{
    internal class SaleEventAllocationAdapter : AdapterBase<ISaleEventAllocation>, ISaleEventAllocation
    {
        public int Id => Adaptee.Id;

        public int SaleEventId
        {
            get => Adaptee.SaleEventId;
            set => Adaptee.SaleEventId = value;
        }

        public int BatchId
        {
            get => Adaptee.BatchId;
            set => Adaptee.BatchId = value;
        }

        public decimal AllocatedQuantity
        {
            get => Adaptee.AllocatedQuantity;
            set => Adaptee.AllocatedQuantity = value;
        }

        public decimal? ReturnedQuantity
        {
            get => Adaptee.ReturnedQuantity;
            set => Adaptee.ReturnedQuantity = value;
        }

        public int UnitId
        {
            get => Adaptee.UnitId;
            set => Adaptee.UnitId = value;
        }

        public DateTime AllocationDt
        {
            get => Adaptee.AllocationDt;
            set => Adaptee.AllocationDt = value;
        }

        public DateTime? ReturnDt
        {
            get => Adaptee.ReturnDt;
            set => Adaptee.ReturnDt = value;
        }

        public int AllocationUserId
        {
            get => Adaptee.AllocationUserId;
            set => Adaptee.AllocationUserId = value;
        }

        public int? ReturnUserId
        {
            get => Adaptee.ReturnUserId;
            set => Adaptee.ReturnUserId = value;
        }

        public IMaterialBatch Batch =>
            Get<IMaterialBatchRepository, IMaterialBatch>("Batch", r => r.GetBatchById(BatchId)?.Batch);

        public IMaterialUnit Unit => Get<IUnitRepository, IMaterialUnit>("Unit", r => r.GetUnit(UnitId));
        public IUser AllocationUser => Get<IUserRepository, IUser>("AllocationUser", r => r.GetUser(AllocationUserId));

        public IUser ReturnUser => Get<IUserRepository, IUser>("ReturnUser",
            r => ReturnUserId == null ? null : r.GetUser(ReturnUserId.Value));

        public ISaleEvent SaleEvent =>
            Get<ISaleEventRepository, ISaleEvent>("SaleEvent", r => r.GetEventById(SaleEventId));

        public SaleEventAllocationAdapter(IServiceLocator serviceLocator, ISaleEventAllocation adaptee) : base(serviceLocator, adaptee)
        {
        }
    }
}
