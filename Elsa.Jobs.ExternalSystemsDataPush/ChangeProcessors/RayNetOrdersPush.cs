using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Integration.Crm.Raynet;
using Elsa.Jobs.Common.EntityChangeProcessing;
using Elsa.Jobs.Common.EntityChangeProcessing.Helpers;
using Elsa.Jobs.ExternalSystemsDataPush.Model;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace Elsa.Jobs.ExternalSystemsDataPush.ChangeProcessors
{
    public class RayNetOrdersPush : IEntityChangeProcessor<OrderExportModel>
    {
        private readonly IRaynetClient _raynet;
        private readonly IDatabase _db;
        private readonly ISession _session;
                
        public RayNetOrdersPush(IRaynetClient raynet, IDatabase db, ISession session, string processorUniqueName)
        {
            _raynet = raynet;
            _db = db;
            _session = session;
            ProcessorUniqueName = processorUniqueName;
        }

        public string ProcessorUniqueName { get; } = "Raynet_Orders_Push";

        public IEnumerable<object> GetComparedValues(IPurchaseOrder e)
        {
            yield return e.OrderStatusId;
            yield return e.PriceWithVat;

            foreach(var i in e.Items)
            {
                yield return i.ErpProductId;
                yield return i.Quantity;
                yield return i.TaxedPrice;
            }
        }

        public IEnumerable<object> GetComparedValues(OrderExportModel e)
        {
            yield return e.StatusId;

            foreach (var i in e.Items)
                yield return i.ItemTaxedPrice;
        }

        public long GetEntityId(OrderExportModel ett)
        {
            return ett.OrderId;
        }

        public EntityChunk<OrderExportModel> LoadChunkToCompare(IDatabase db, int projectId, EntityChunk<OrderExportModel> previousChunk, int alreadyProcessedRowsCount)
        {
            var orders = new Dictionary<long, OrderExportModel>(100);

            Func<System.Data.Common.DbDataReader, OrderItemExportModel> orderItemParser = null;
            Func<System.Data.Common.DbDataReader, OrderExportModel> orderParser = null;

            db.Sql()
                .Call("GetOrdersToDataPush")
                .WithParam("@projectId", projectId)
                .WithParam("@take", 100)
                .WithParam("@skip", alreadyProcessedRowsCount)                
                .ReadRows(row => {

                    orderItemParser = orderItemParser ?? row.GetRowParser<OrderItemExportModel>(typeof(OrderItemExportModel));

                    var orderItem = orderItemParser(row);

                    if(orders.TryGetValue(orderItem.OrderId, out var order)) 
                    {
                        orderParser = orderParser ?? row.GetRowParser<OrderExportModel>(typeof(OrderExportModel));
                        order = orderParser(row);
                        orders[orderItem.OrderId] = order;
                    }

                    order.Items.Add(orderItem);
                });

            return new EntityChunk<OrderExportModel>(orders.Values.ToList(), orders.Count < 100);
        }

        public void Process(IEnumerable<EntityChangeEvent<OrderExportModel>> changedEntities, IEntityProcessCallback<OrderExportModel> callback, ILog log)
        {
            foreach(var orderEvent in changedEntities) 
            {
                if (orderEvent.IsNew && orderEvent.Entity.OrderStatusId != 5)
                    continue; // shouldnt send unsuccessful orders 
            }
        }
    }
}
