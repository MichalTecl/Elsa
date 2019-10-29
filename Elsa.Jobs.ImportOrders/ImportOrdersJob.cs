using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Jobs.Common;

using Newtonsoft.Json;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire.RobOrm.Core;

namespace Elsa.Jobs.ImportOrders
{
    public class ImportOrdersJob : IExecutableJob
    {
        private readonly IErpClientFactory m_erpClientFactory;
        private readonly IPurchaseOrderRepository m_purchaseOrderRepository;
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        private readonly ILog m_log;
        private readonly IStockEventRepository m_stockEventRepository;
        
        public ImportOrdersJob(IErpClientFactory erpClientFactory, IDatabase database, ISession session, IPurchaseOrderRepository purchaseOrderRepository, ILog log, IStockEventRepository stockEventRepository)
        {
            m_erpClientFactory = erpClientFactory;
            m_database = database;
            m_session = session;
            m_purchaseOrderRepository = purchaseOrderRepository;
            m_log = log;
            m_stockEventRepository = stockEventRepository;
        }

        public void Run(string customDataJson)
        {
            var cuData = JsonConvert.DeserializeObject<ImportOrdersCustomData>(customDataJson);

            var erp = m_erpClientFactory.GetErpClient(cuData.ErpId);

           m_log.Info($"ERP = {erp.Erp.Description}");

            try
            {
                var minDate =
                    m_database.Sql()
                        .Execute("SELECT MAX(PurchaseDate) FROM PurchaseOrder WHERE ProjectId = @p AND ErpId = @e")
                        .WithParam("@p", m_session.Project.Id)
                        .WithParam("@e", erp.Erp.Id)
                        .Scalar();

                var startDate = erp.CommonSettings.HistoryStart;
                if ((minDate != null) && (DBNull.Value != minDate))
                {
                    startDate = (DateTime)minDate;

                    if (startDate > DateTime.Now.AddDays(-1 * erp.CommonSettings.OrderSyncHistoryDays))
                    {
                        startDate = DateTime.Now.AddDays(-1 * erp.CommonSettings.OrderSyncHistoryDays);
                    }
                }

                if (cuData.HistoryDepthDays != null)
                {
                    startDate = startDate.AddDays(-1 * cuData.HistoryDepthDays.Value);
                    if (startDate < erp.CommonSettings.HistoryStart)
                    {
                        startDate = erp.CommonSettings.HistoryStart;
                    }
                }
                
                while (startDate < DateTime.Now)
                {
                    var queryDays = erp.CommonSettings.MaxQueryDays;

                    while (queryDays > 0)
                    {
                        try
                        {
                            var endDate = startDate.AddDays(queryDays);

                            m_purchaseOrderRepository.PreloadOrders(startDate, endDate);

                            m_log.Info($"Stahuji objednavky {startDate} - {endDate}");

                            var erpOrders = erp.LoadOrders(startDate, endDate).ToList();

                            m_log.Info($"Stazeno {erpOrders.Count} zaznamu");

                            foreach (var srcOrder in erpOrders)
                            {
                                m_purchaseOrderRepository.ImportErpOrder(srcOrder);
                            }

                            m_log.Info("Ulozeno");

                            startDate = endDate;
                            break;
                        }
                        catch (Exception ex)
                        {
                            queryDays = queryDays / 2;
                            
                            if (queryDays < 1)
                            {
                                m_log.Error($"Nepodarilo se stahnout objednavky: {ex.Message}");
                                throw;
                            }
                            else
                            {
                                m_log.Info($"Stazeni objednavek z ERP selhalo, zkracuji interval dotazu na {queryDays} dnu");
                            }
                        }
                    }
                }

                ProcessReturns();

               m_log.Info("Job dokoncen");

            }
            finally
            {
                (erp as IDisposable)?.Dispose();
            }
        }

        private void ProcessReturns()
        {
            try
            {
                m_log.Info("Zacinam zpracovavat vracene objednavky");

                var ordids = new List<long>();
                m_database.Sql().ExecuteWithParams(@"select distinct po.Id
                                                          from PurchaseOrder po
                                                          inner join OrderItem     oi ON (oi.PurchaseOrderId = po.Id)
                                                          left join  OrderItem     ki ON (ki.KitParentId = oi.Id)
                                                          inner join OrderItemMaterialBatch omb ON (omb.OrderItemId = ISNULL(ki.Id, oi.Id))
                                                        where po.ProjectId = {0}
                                                          and po.ReturnDt is not null
                                                          and not exists(select top 1 1
                                                                           from MaterialStockEvent evt
				                                                           join StockEventType st on evt.TypeId = st.Id
				                                                           where evt.SourcePurchaseOrderId = po.Id)", m_session.Project.Id).ReadRows<long>(ordids.Add);

                if (!ordids.Any())
                {
                    m_log.Info("Zadne vratky");
                    return;
                }

                var targetEventType = m_stockEventRepository.GetAllEventTypes()
                    .FirstOrDefault(e => e.GenerateForReturnedOrders == true);

                if (targetEventType == null)
                {
                    m_log.Info("Neni zadny StockEventType.GenerateForReturnedOrders");
                    return;
                }

                foreach (var ordid in ordids)
                {
                    try
                    {
                        using (var tx = m_database.OpenTransaction())
                        {
                            var order = m_purchaseOrderRepository.GetOrder(ordid);

                            m_log.Info($"Generuji odpis pro vracenou objednavku {order.OrderNumber}");

                            m_stockEventRepository.MoveOrderToEvent(ordid, targetEventType.Id,
                                $"Vráceno z objednávky {order.OrderNumber}");

                            tx.Commit();
                        }

                        m_log.Info("Hotovo");
                    }
                    catch (Exception ex)
                    {
                        m_log.Error($"Chyba pri zpracovani vratky pro objednavku ID = {ordid}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                m_log.Error("Chyba pri zpracovani vratek", ex);
            }
        }
    }
}
