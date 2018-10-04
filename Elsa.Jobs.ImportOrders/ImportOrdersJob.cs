using System;
using System.Linq;

using Elsa.Jobs.Common;

using Newtonsoft.Json;

using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Logging;

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
        
        public ImportOrdersJob(IErpClientFactory erpClientFactory, IDatabase database, ISession session, IPurchaseOrderRepository purchaseOrderRepository, ILog log)
        {
            m_erpClientFactory = erpClientFactory;
            m_database = database;
            m_session = session;
            m_purchaseOrderRepository = purchaseOrderRepository;
            m_log = log;
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
                    var endDate = startDate.AddDays(erp.CommonSettings.MaxQueryDays);

                    m_purchaseOrderRepository.PreloadOrders(startDate, endDate);

                    m_log.Info($"Stahuji objednavky {startDate} - {endDate}");

                    var erpOrders = erp.LoadOrders(startDate, endDate).ToList();

                    m_log.Info($"Stazeno {erpOrders.Count} zaznamu");
                    
                    using (var trx = m_database.OpenTransaction())
                    {
                        foreach (var srcOrder in erpOrders)
                        {
                            m_purchaseOrderRepository.ImportErpOrder(srcOrder);
                        }

                        trx.Commit();
                        m_log.Info("Ulozeno");
                    }

                    startDate = endDate;
                }

               m_log.Info("Job dokoncen");

            }
            finally
            {
                (erp as IDisposable)?.Dispose();
            }
        }
    }
}
