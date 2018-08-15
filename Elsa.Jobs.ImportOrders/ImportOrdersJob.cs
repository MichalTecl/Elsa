using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Jobs.Common;

using Newtonsoft.Json;

using Robowire;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Commerce;

using Robowire.RobOrm.Core;

namespace Elsa.Jobs.ImportOrders
{
    public class ImportOrdersJob : IExecutableJob
    {
        private readonly IErpClientFactory m_erpClientFactory;
        private readonly IPurchaseOrderRepository m_purchaseOrderRepository;
        private readonly IDatabase m_database;
        private readonly ISession m_session;
        
        public ImportOrdersJob(IErpClientFactory erpClientFactory, IDatabase database, ISession session, IPurchaseOrderRepository purchaseOrderRepository)
        {
            m_erpClientFactory = erpClientFactory;
            m_database = database;
            m_session = session;
            m_purchaseOrderRepository = purchaseOrderRepository;
        }

        public void Run(string customDataJson)
        {
            var cuData = JsonConvert.DeserializeObject<ImportOrdersCustomData>(customDataJson);

            var erp = m_erpClientFactory.GetErpClient(cuData.ErpId);

            try
            {
                var minDate =
                    m_database.Sql()
                        .Execute("SELECT MAX(PurchaseDate) FROM PurchaseOrder WHERE ProjectId = @p")
                        .WithParam("@p", m_session.Project.Id)
                        .Scalar();

                /*var minDate =
                    m_database.ExecuteScalar(
                        "SELECT MAX(PurchaseDate) FROM PurchaseOrder WHERE ProjectId = @p",
                        p => ((SqlParameterCollection)p).AddWithValue("@p", m_session.Project.Id));*/

                var startDate = erp.CommonSettings.HistoryStart;
                if ((minDate != null) && (DBNull.Value != minDate))
                {
                    startDate = (DateTime)minDate;

                    if (startDate > DateTime.Now.AddDays(-1 * erp.CommonSettings.OrderSyncHistoryDays))
                    {
                        startDate = DateTime.Now.AddDays(-1 * erp.CommonSettings.OrderSyncHistoryDays);
                    }
                }

                while (startDate < DateTime.Now)
                {
                    var endDate = startDate.AddDays(erp.CommonSettings.MaxQueryDays);

                    m_purchaseOrderRepository.PreloadOrders(startDate, endDate);

                    Console.WriteLine($"Stahuji objednavky {startDate} - {endDate}");

                    var erpOrders = erp.LoadOrders(startDate, endDate).ToList();

                    Console.WriteLine($"Stazeno {erpOrders.Count} zaznamu");
                    
                    using (var trx = m_database.OpenTransaction())
                    {
                        foreach (var srcOrder in erpOrders)
                        {
                            m_purchaseOrderRepository.ImportErpOrder(srcOrder);
                        }

                        trx.Commit();
                        Console.WriteLine("Ulozeno");
                    }

                    startDate = endDate;
                }

               Console.WriteLine("a je to");

            }
            finally
            {
                (erp as IDisposable)?.Dispose();
            }



            ;
        }
    }
}
