﻿using Elsa.Common.Interfaces;
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
using Elsa.Jobs.ExternalSystemsDataPush.Mappers;
using Elsa.Integration.Crm.Raynet.Model;

namespace Elsa.Jobs.ExternalSystemsDataPush.ChangeProcessors
{
    public class RayNetOrdersPush : IEntityChangeProcessor<OrderExportModel>
    {
        private readonly IRaynetClient _raynet;
        private readonly IDatabase _db;
        private readonly ISession _session;

        private List<ProductListItem> _productList = null;
        public List<ProductListItem> GetProductList()
        {
            if (_productList == null)
            {
                _productList = _raynet.GetProductList().Data;
            }

            return _productList;
        }

        private HashSet<string> _productsOrderedInLastYear = null;
        private HashSet<string> GetProductsOrderedInLastYear()
        {
            if (_productsOrderedInLastYear == null)
            {               
                var query = "SELECT DISTINCT oi.PlacedName "
                           + " FROM PurchaseOrder po"
                           + " JOIN vwOrderItems voi ON (po.Id = voi.OrderId)"
                           + " JOIN OrderItem oi ON (voi.OrderItemId = oi.Id)"
                           + " WHERE po.ProjectId = {0} AND po.PurchaseDate > DATEADD(year, -1, GETDATE())";

                _productsOrderedInLastYear 
                    = new HashSet<string>(_db.Sql()
                    .ExecuteWithParams(query, _session.Project.Id)
                    .MapRows(r => r.GetString(0)));
            }

            return _productsOrderedInLastYear;
        }

        public RayNetOrdersPush(IRaynetClient raynet, IDatabase db, ISession session)
        {
            _raynet = raynet;
            _db = db;
            _session = session;
        }

        public string ProcessorUniqueName { get; } = "Raynet_Orders_Push";
                
        public IEnumerable<object> GetComparedValues(OrderExportModel e)
        {
            yield return e.OrderStatusId;
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

                    if(!orders.TryGetValue(orderItem.OrderId, out var order)) 
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
                try
                {                    
                    if (orderEvent.IsNew)
                    {
                        if (orderEvent.Entity.OrderStatusId != 5)
                        {
                            log.Info($"Order {orderEvent.Entity.OrderNr} is not sent to RN due to unsuccessful status");
                            continue; // shouldnt send unsuccessful orders
                        }
                                                
                        log.Info($"Order {orderEvent.Entity.OrderNr} is successfuly finished -> sending to RN");

                        var businessCase = OrderMapper.ToBcModel(orderEvent.Entity, GetProductList(), GetProductsOrderedInLastYear(), log);
                        if (businessCase == null) 
                        {
                            log.Info($"It was not possible to map {orderEvent.Entity.OrderNr} -> skipping sending to RN");
                            continue;
                        }

                        var existingCase = _raynet.GetBusinessCases(businessCase.Name).Data.FirstOrDefault();
                        if (existingCase != null) 
                        {
                            log.Info($"BusinessCase with Name={businessCase.Name} already exists in RN");
                            if (existingCase.Company?.Id != businessCase.Company.Id) 
                            {
                                log.Error($"Case {businessCase.Name} in RN belongs to companyId={existingCase.Company?.Id} but in Elsa it belongs to RN Company.id={businessCase.Company?.Id}");
                            }

                            continue;
                        }

                        var response = _raynet.CreateBusinessCase(businessCase);
                        callback.OnProcessed(orderEvent.Entity, response.Data.Id.ToString(), null);
                    }
                    else
                    {
                        log.Info($"Order {orderEvent.Entity.OrderNr} has changed -> sending to RN");
                        _raynet.ChangeBcValidity(long.Parse(orderEvent.ExternalId), orderEvent.Entity.OrderStatusId == 5);
                        callback.OnProcessed(orderEvent.Entity, orderEvent.ExternalId, null);
                    }
                }
                catch(Exception ex) 
                {
                    log.Error($"Cannot sync order {orderEvent.Entity.OrderNr} to RN", ex);
                }
            }
        }
    }
}
