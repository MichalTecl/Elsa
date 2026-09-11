using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Jobs.Common;

using Newtonsoft.Json;
using System.Text;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Robowire.RobOrm.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Smtp.Core;
using Elsa.Smtp.Core.Database;

namespace Elsa.Jobs.ImportOrders
{
    public class ImportOrdersJob : IExecutableJob, IAdHocOrdersSyncProvider
    {
        private const string FAILURE_RECIPIENT_GROUP = "Selhani importu objednavek";

        private readonly IErpClientFactory _erpClientFactory;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ILog _log;
        private readonly IStockEventRepository _stockEventRepository;
        private readonly IOrderImportFailureRepository _orderImportFailureRepository;
        private readonly IMailSender _mailSender;
        private readonly IRecipientListsRepository _recipientListsRepository;
        
        public ImportOrdersJob(IErpClientFactory erpClientFactory, IDatabase database, ISession session, IPurchaseOrderRepository purchaseOrderRepository, ILog log, IStockEventRepository stockEventRepository, IOrderImportFailureRepository orderImportFailureRepository, IMailSender mailSender, IRecipientListsRepository recipientListsRepository)
        {
            _erpClientFactory = erpClientFactory;
            _database = database;
            _session = session;
            _purchaseOrderRepository = purchaseOrderRepository;
            _log = log;
            _stockEventRepository = stockEventRepository;
            _orderImportFailureRepository = orderImportFailureRepository;
            _mailSender = mailSender;
            _recipientListsRepository = recipientListsRepository;
        }

        public void Run(string customDataJson)
        {
            var cuData = JsonConvert.DeserializeObject<ImportOrdersCustomData>(customDataJson);

            var erp = _erpClientFactory.GetErpClient(cuData.ErpId);

            _log.Info($"ERP = {erp.Erp.Description}");
            
            try
            {
                InSyncSession(erp.Erp.Id, () =>
                {
                    RetryFailedOrders(erp);

                    if (erp.CommonSettings.UseIncrementalOrderChangeMode)
                    {
                        _log.Info("erp.CommonSettings.AllowsChangedFromOrdersDownload = true -> starting importing orders changed after last sync");
                        DownloadOrdersInIncrementalMode(erp);
                    }
                    else
                    {
                        _log.Info("erp.CommonSettings.AllowsChangedFromOrdersDownload = false -> starting importing orders from - to by purchase date");
                        DownloadOrdersInSnapshotMode(cuData, erp);
                    }
                });

                ProcessReturns();                

                _log.Info("Job dokoncen");
            }
            finally
            {
                SendFailureNotifications();
                (erp as IDisposable)?.Dispose();
            }
        }

        private void InSyncSession(int erpId, Action a)
        {
            var syncSessionId = _purchaseOrderRepository.StartSyncSession(erpId);
            a();
            _purchaseOrderRepository.EndSyncSession(syncSessionId);
        }

        public void SyncPaidOrders()
        {
            try
            {
                SyncPaidOrdersCore();
            }
            finally
            {
                SendFailureNotifications();
            }
        }

        private void SyncPaidOrdersCore()
        {
            _log.Info("Requested SyncPaidOrders");

            foreach(var erp in _erpClientFactory.GetAllErpClients())
            {
                try
                {
                    _log.Info($"Starting syncing paid orders from {erp.Erp.Description}");

                    if (erp.CommonSettings.UseIncrementalOrderChangeMode)
                    {
                        _log.Info($"ERP {erp.Erp.Description} is set to {nameof(erp.CommonSettings.UseIncrementalOrderChangeMode)} = true => the paid orders sync is replaced by complete orders sync");

                        InSyncSession(erp.Erp.Id, () => {
                            RetryFailedOrders(erp);
                            DownloadOrdersInIncrementalMode(erp);
                        });                        
                    }
                    else
                    {
                        var paidOrders = erp.LoadPaidOrders(DateTime.Now.AddDays(-1 * erp.CommonSettings.PaidOrdersSyncHistoryDays), DateTime.Now.AddDays(1)).ToList();
                        _purchaseOrderRepository.ImportErpOrders(erp.Erp.Id, paidOrders);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Faild attempt to sync paid orders from erp {erp.Erp.Description}: {ex.Message}", ex);
                }
            }
        }

        private void SendFailureNotifications()
        {
            try
            {
                SendFailureNotificationsCore();
            }
            catch (Exception ex)
            {
                _log.Error("Zpracování e-mailových upozornění na selhání importu objednávek se nezdařilo. Odeslání bude zopakováno při příštím importu.", ex);
            }
        }

        private void SendFailureNotificationsCore()
        {
            var failures = _orderImportFailureRepository.GetNotNotifiedForCurrentProject();
            if (failures.Count == 0)
                return;

            var body = new StringBuilder();
            body.AppendLine($"Během importu objednávek bylo nově zaznamenáno {failures.Count} selhání.");
            body.AppendLine();

            foreach (var failure in failures)
            {
                body.AppendLine($"ERP: {failure.Erp?.Description ?? failure.ErpId.ToString()}");
                body.AppendLine($"Objednávka: {failure.OrderNumber}");
                body.AppendLine($"První selhání: {failure.FirstFailureDt:dd.MM.yyyy HH:mm:ss}");
                body.AppendLine($"Poslední selhání: {failure.LastFailureDt:dd.MM.yyyy HH:mm:ss}");
                body.AppendLine($"Počet zaznamenaných selhání: {failure.FailureCount}");
                body.AppendLine(failure.ResolveDate == null
                    ? "Stav: čeká na úspěšný import"
                    : $"Stav: vyřešeno {failure.ResolveDate:dd.MM.yyyy HH:mm:ss}");
                body.AppendLine("Chyba:");
                body.AppendLine(failure.LastError ?? "Bez detailu chyby");
                body.AppendLine();
                body.AppendLine(new string('-', 72));
                body.AppendLine();
            }

            try
            {
                if (!_recipientListsRepository.GetRecipients(FAILURE_RECIPIENT_GROUP).Any())
                    throw new InvalidOperationException($"Skupina příjemců '{FAILURE_RECIPIENT_GROUP}' nemá žádnou e-mailovou adresu.");

                _mailSender.SendToGroup(
                    SenderMailboxType.SystemRobot,
                    FAILURE_RECIPIENT_GROUP,
                    $"Selhání importu objednávek ({failures.Count})",
                    body.ToString());

                _orderImportFailureRepository.MarkNotificationsSent(failures.Select(f => f.Id));
            }
            catch (Exception ex)
            {
                _log.Error("Odeslání souhrnu selhání importu objednávek se nezdařilo. Odeslání bude zopakováno při příštím importu.", ex);
            }
        }

        private void DownloadOrdersInIncrementalMode(IErpClient erp)
        {
            var lastSyncDt = _purchaseOrderRepository.GetLastSuccessSyncDt(erp.Erp.Id) ?? DateTime.MinValue;
            _log.Info($"LastSyncDt={lastSyncDt}");

            var hardMinDt = DateTime.Now.AddDays(-1 * erp.CommonSettings.MaxQueryDays);
            if (lastSyncDt < hardMinDt)
            {
                lastSyncDt = hardMinDt;
                _log.Info($"DT of last incremental sync is before {hardMinDt} - using configured value {nameof(erp.CommonSettings.MaxQueryDays)} => {lastSyncDt}");
            }
                        
            // to avoid edge cases
            lastSyncDt = lastSyncDt.AddMinutes(-1);
            _log.Info($"Final lastSyncDt considered to be '{lastSyncDt}'");

            _log.Info($"Requesting ERP orders changedFrom={lastSyncDt}");
            var orders = erp.LoadOrdersIncremental(lastSyncDt).ToList();

            _purchaseOrderRepository.ImportErpOrders(erp.Erp.Id, orders);
        }

        private void RetryFailedOrders(IErpClient erp)
        {
            var failedOrders = _orderImportFailureRepository.GetPendingForErp(erp.Erp.Id);
            if (failedOrders.Count == 0)
                return;

            _log.Info($"Retrying {failedOrders.Count} previously failed orders from ERP {erp.Erp.Description}");

            foreach (var failure in failedOrders)
            {
                try
                {
                    _log.Info($"Retrying import of order {failure.OrderNumber}");

                    var order = erp.LoadOrder(failure.OrderNumber);
                    if (order == null)
                        throw new Exception($"ERP returned no data for order {failure.OrderNumber}");

                    _purchaseOrderRepository.ImportErpOrder(order);
                    _orderImportFailureRepository.Resolve(erp.Erp.Id, failure.OrderNumber);

                    _log.Info($"Retry of order {failure.OrderNumber} completed successfully");
                }
                catch (Exception ex)
                {
                    _orderImportFailureRepository.RegisterFailure(
                        erp.Erp.Id,
                        failure.OrderNumber,
                        ex.ToString());

                    _log.Error($"Retry of order {failure.OrderNumber} failed. Other orders will continue to be imported.", ex);
                }
            }
        }

        private void DownloadOrdersInSnapshotMode(ImportOrdersCustomData cuData, IErpClient erp)
        {
            var erpSyncMinDate = DateTime.Now.AddDays(-1 * erp.CommonSettings.OrderSyncHistoryDays);

            var minDate =
                _database.Sql()
                    .Execute("SELECT MAX(PurchaseDate) FROM PurchaseOrder WHERE ProjectId = @p AND ErpId = @e")
                    .WithParam("@p", _session.Project.Id)
                    .WithParam("@e", erp.Erp.Id)
                    .Scalar();

            var startDate = erp.CommonSettings.HistoryStart;
            if ((minDate != null) && (DBNull.Value != minDate))
            {
                startDate = (DateTime)minDate;

                if (startDate > erpSyncMinDate)
                {
                    startDate = erpSyncMinDate;
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

                        _log.Info($"Stahuji objednavky {startDate} - {endDate}");

                        var erpOrders = erp.LoadOrdersSnapshot(startDate, endDate).ToList();

                        _purchaseOrderRepository.ImportErpOrders(erp.Erp.Id, erpOrders);

                        startDate = endDate;
                        break;
                    }
                    catch (Exception ex)
                    {
                        queryDays = queryDays / 2;

                        if (queryDays < 1)
                        {
                            _log.Error($"Nepodarilo se stahnout objednavky: {ex.Message}");
                            throw;
                        }
                        else
                        {
                            _log.Info($"Stazeni objednavek z ERP selhalo, zkracuji interval dotazu na {queryDays} dnu");
                        }
                    }
                }
            }
        }
                        
        private void ProcessReturns()
        {
            try
            {
                _log.Info("Zacinam zpracovavat vracene objednavky");

                var ordids = new List<long>();
                _database.Sql().ExecuteWithParams(@"select distinct po.Id
                                                          from PurchaseOrder po
                                                          inner join OrderItem     oi ON (oi.PurchaseOrderId = po.Id)
                                                          left join  OrderItem     ki ON (ki.KitParentId = oi.Id)
                                                          inner join OrderItemMaterialBatch omb ON (omb.OrderItemId = ISNULL(ki.Id, oi.Id))
                                                        where po.ProjectId = {0}
                                                          and po.ReturnDt is not null
                                                          and not exists(select top 1 1
                                                                           from MaterialStockEvent evt
				                                                           join StockEventType st on evt.TypeId = st.Id
				                                                           where evt.SourcePurchaseOrderId = po.Id)", _session.Project.Id).ReadRows<long>(ordids.Add);

                if (!ordids.Any())
                {
                    _log.Info("Zadne vratky");
                    return;
                }

                var targetEventType = _stockEventRepository.GetAllEventTypes()
                    .FirstOrDefault(e => e.GenerateForReturnedOrders == true);

                if (targetEventType == null)
                {
                    _log.Info("Neni zadny StockEventType.GenerateForReturnedOrders");
                    return;
                }

                foreach (var ordid in ordids)
                {
                    try
                    {
                        using (var tx = _database.OpenTransaction())
                        {
                            var order = _purchaseOrderRepository.GetOrder(ordid);

                            _log.Info($"Generuji odpis pro vracenou objednavku {order.OrderNumber}");

                            _stockEventRepository.MoveOrderToEvent(ordid, targetEventType.Id,
                                $"Vráceno z objednávky {order.OrderNumber}");

                            tx.Commit();
                        }

                        _log.Info("Hotovo");
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"Chyba pri zpracovani vratky pro objednavku ID = {ordid}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Chyba pri zpracovani vratek", ex);
            }
        }
    }
}
