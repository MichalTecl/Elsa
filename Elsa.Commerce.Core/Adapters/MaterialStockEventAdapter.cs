using System;
using System.Linq;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire;

namespace Elsa.Commerce.Core.Adapters
{
    internal class MaterialStockEventAdapter : AdapterBase<IMaterialStockEvent>, IMaterialStockEvent
    {
        public MaterialStockEventAdapter(IServiceLocator serviceLocator, IMaterialStockEvent adaptee) : base(serviceLocator, adaptee)
        {
        }

        public int ProjectId { get; set; }
        public IProject Project { get; }
        public int Id => Adaptee.Id;
        public int BatchId { get => Adaptee.BatchId; set => Adaptee.BatchId = value; }

        public IMaterialBatch Batch =>
            Get<IMaterialBatchRepository, IMaterialBatch>("Batch", r => r.GetBatchById(BatchId)?.Batch);

        public int UnitId { get => Adaptee.UnitId; set => Adaptee.UnitId = value; }
        public IMaterialUnit Unit => Get<IUnitRepository, IMaterialUnit>("Unit", r => r.GetUnit(UnitId));

        public decimal Delta
        {
            get => Adaptee.Delta;
            set => Adaptee.Delta = value;
        }

        public int TypeId
        {
            get => Adaptee.TypeId;
            set => Adaptee.Delta = value;
        }

        public IStockEventType Type => Get<IStockEventRepository, IStockEventType>("Type",
            r => r.GetAllEventTypes().FirstOrDefault(t => t.Id == TypeId));

        public string Note
        {
            get => Adaptee.Note;
            set => Adaptee.Note = value;
        }

        public int UserId
        {
            get => Adaptee.UserId;
            set => Adaptee.UserId = value;
        }

        public IUser User => Get<IUserRepository, IUser>("User", r => r.GetUser(UserId));

        public DateTime EventDt
        {
            get => Adaptee.EventDt;
            set => Adaptee.EventDt = value;
        }

        public string EventGroupingKey
        {
            get => Adaptee.EventGroupingKey;
            set => Adaptee.EventGroupingKey = value;
        }

        public long? SourcePurchaseOrderId
        {
            get => Adaptee.SourcePurchaseOrderId;
            set => Adaptee.SourcePurchaseOrderId = value;
        }

        public IPurchaseOrder SourcePurchaseOrder => Get<IPurchaseOrderRepository, IPurchaseOrder>(
            "SourcePurchaseOrder", r => SourcePurchaseOrderId == null ? null : r.GetOrder(SourcePurchaseOrderId.Value));
    }
}
