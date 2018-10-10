using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.Erp.Elerp.Model;
using Elsa.Integration.Erp.Flox;

using Newtonsoft.Json;

namespace Elsa.Integration.Erp.Elerp
{
    public class ElerpClient : IErpClient
    {
        private readonly ElerpConfig m_config = new ElerpConfig();

        public IErp Erp { get; set; }

        public IErpDataMapper Mapper { get; } = new ElerpDataMapper();

        public IErpCommonSettings CommonSettings => m_config;

        public IEnumerable<IErpOrderModel> LoadOrders(DateTime @from, DateTime? to = null)
        {
            return GetAllOrders().Where(o => o.Dt >= from && o.Dt <= (to ?? DateTime.MaxValue));
        }

        public IEnumerable<IErpOrderModel> LoadPaidOrders(DateTime @from, DateTime to)
        {
            return LoadOrders(from, to).Where(m => m.ErpStatus == OrderStatus.ReadyToPack.Id.ToString());
        }

        public void MarkOrderPaid(IPurchaseOrder po)
        {
            var o = LoadOrder(po.OrderNumber) as ElerpOrderModel;
            if (o == null)
            {
                throw new InvalidOperationException($"Elerp doesn't have order with number '{po.OrderNumber}'");
            }

            o.ErpStatus = OrderStatus.ReadyToPack.Id.ToString();

            SaveOrder(o);
        }

        public IErpOrderModel LoadOrder(string orderNumber)
        {
            return GetAllOrders().FirstOrDefault(o => o.OrderNumber == orderNumber);
        }

        public void MakeOrderSent(IPurchaseOrder po)
        {
            var o = LoadOrder(po.OrderNumber) as ElerpOrderModel;
            if (o == null)
            {
                throw new InvalidOperationException($"Elerp doesn't have order with number '{po.OrderNumber}'");
            }

            o.ErpStatus = OrderStatus.Sent.Id.ToString();

            SaveOrder(o);
        }

        private IEnumerable<ElerpOrderModel> GetAllOrders()
        {
            foreach (var filePath in Directory.GetFiles(m_config.DataDir))
            {
                using (var strm = File.OpenRead(filePath))
                using(var rdr = new StreamReader(strm, Encoding.UTF8))
                {
                    yield return JsonConvert.DeserializeObject<ElerpOrderModel>(rdr.ReadToEnd());
                }
            }
        }

        public void SaveOrder(IErpOrderModel source)
        {
            var fileName = Path.Combine(m_config.DataDir, $"{source.OrderNumber}.json");
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            source.ErpSystemId = Erp.Id;
            File.WriteAllText(fileName, JsonConvert.SerializeObject(source));
        }
    }
}
