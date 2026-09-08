using Elsa.Commerce.Core;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;
using Robowire.RobOrm.Core;
using System;
using System.Linq;

namespace Elsa.Jobs.OrdersPostprocessing.Steps
{
    public abstract class ProcessStepBase : IOrdersPostprocessStep
    {
        private const int MAX_PROCESSING_LOG_MESSAGE_LENGTH = 1000;

        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ILog _log;
        private readonly IOrdersFacade _ordersFacade;

        protected abstract string ProcessCode { get; }

        protected abstract int HistoryDepthDays { get; }

        protected virtual int PageSize { get; } = 1000;

        protected virtual bool SinglePagePerRun { get; } = false; 

        protected abstract IOrderStatus[] SourceOrderStatuses { get; }

        protected ProcessStepBase(IDatabase database, ISession session, ILog log, IOrdersFacade ordersFacade)
        {
            _database = database;
            _session = session;
            _log = log;
            _ordersFacade = ordersFacade;
        }

        public void Process(Action<string> onError)
        {
            _log.Info($"Starting orders postprocessing step {this}");

            var minDate = DateTime.Now.AddDays(-1 * HistoryDepthDays);

            
            _log.Info($"Loading orders placed after {minDate}");

            long lastSeenId = -1;

            var statuses = SourceOrderStatuses.Select(s => s.Id).ToList();

            while (true)
            {
                var query = _database
                    .SelectFrom<IPurchaseOrder>()
                    .OrderBy(o => o.Id)
                    .Where(o => o.ProjectId == _session.Project.Id)
                    .Where(o => o.PurchaseDate > minDate)
                    .Where(o => o.Id > lastSeenId);
                    
                if (statuses.Count > 0)
                {
                    query = query.Where(o => o.OrderStatusId.InCsv(statuses));
                }

                if (!string.IsNullOrWhiteSpace(ProcessCode))
                {
                    var codeQuery = _database.SelectFrom<IOrderProcessingLog>()
                        .Where(l => l.ProcessCode == ProcessCode)
                        .Transform(q => q.PurchaseOrderId);

                    query = query.Where(o => o.Id.NotInSubquery(codeQuery));
                }

                query = QueryOrders(query).Take(PageSize);

                var batch = query.Execute().ToList();

                if (batch.Count == 0)
                {
                    _log.Info($"No more orders to process, step {this} done");
                    return;
                }

                _log.Info($"Loaded {batch.Count} of orders");

                foreach (var order in batch) 
                {
                    lastSeenId = Math.Max(lastSeenId, order.Id);

                    ExecuteOrderTransaction(order, onError);
                }

                _log.Info($"Processed batch of {batch.Count} orders");

                if (SinglePagePerRun)
                {
                    _log.Info($"{nameof(SinglePagePerRun)} is true - step done");
                    return;
                }
            }
        }
                
        protected abstract IQueryBuilder<IPurchaseOrder> QueryOrders(IQueryBuilder<IPurchaseOrder> query);

        private void ExecuteOrderTransaction(IPurchaseOrder order, Action<string> onError)
        {
            using (var tx = _database.OpenTransaction())
            {
                try
                {
                    string message = null;

                    _log.Info($"Starting processing of order ID={order.Id} #{order.OrderNumber}");

                    var result = TryProcessOrder(order, m => message = m);

                    _log.Info($"Result of processing of order ID={order.Id} #{order.OrderNumber} = {result}");

                    if (result && (!string.IsNullOrEmpty(ProcessCode)))
                    {
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            throw new InvalidOperationException(
                                $"Krok {this} nevyplnil popis záznamu o zpracování objednávky.");
                        }

                        if (message.Length > MAX_PROCESSING_LOG_MESSAGE_LENGTH)
                        {
                            throw new InvalidOperationException(
                                $"Popis záznamu o zpracování objednávky překročil maximální délku {MAX_PROCESSING_LOG_MESSAGE_LENGTH} znaků.");
                        }

                        _ordersFacade.LogOrderProcess(order.Id, ProcessCode, message);
                    }

                    tx.Commit();
                }
                catch (Exception ex)
                {
                    _log.Error($"Processing of order ID={order.Id} #{order.OrderNumber} failed", ex);
                    onError(ex.Message);
                }
            }
        }

        protected abstract bool TryProcessOrder(IPurchaseOrder order, Action<string> processingLogMessageWriter);

        public override string ToString() => string.IsNullOrEmpty(ProcessCode) ? this.GetType().Name : ProcessCode;        
    }
}
