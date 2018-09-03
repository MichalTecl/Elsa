using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Common.Communication;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.Erp.Flox.Protocol;
using Elsa.Integration.Erp.Flox.Protocol.OrderModel;

namespace Elsa.Integration.Erp.Flox
{
    public class FloxClient : IErpClient, IDisposable
    {
        private readonly ILog m_log;

        private readonly FloxClientConfig m_config;

        private string m_csrfToken;

        private DateTime m_lastAccess = DateTime.MinValue;

        private readonly WebFormsClient m_client = new WebFormsClient();

        public FloxClient(FloxClientConfig config, FloxDataMapper mapper, ILog log)
        {
            m_config = config;
            Mapper = mapper;
            m_log = log;
        }

        public IErp Erp { get; set; }

        public IErpDataMapper Mapper { get; }

        public IErpCommonSettings CommonSettings => m_config;

        public IEnumerable<IErpOrderModel> LoadOrders(DateTime from, DateTime? to = null)
        {
            m_log.Info($"Zacinam stahovani objednavek od={from}, do={to}");
            EnsureSession();

            var xDateFrom = from.ToString("d.+M.+yyyy");
            var nDateFrom = from.ToString("yyyy-MM-dd");
            var dlToken = ((long)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)).ToString();

            var fields = new Dictionary<string, string>
                             {
                                 ["pur_date_from_xdate"] = xDateFrom,
                                 ["pur_date_from"] = nDateFrom
                             };

            if (to != null)
            {
                fields["pur_date_to_xdate"] = to.Value.ToString("d.+M.+yyyy");
                fields["pur_date_to"] = to.Value.ToString("yyyy-MM-dd");
            }

            //if (status != null)
            //{
            //    throw new Exception("TODO - Flox status");
            //}

            fields["downloadToken"] = dlToken;

            var post = m_client.Post(ActionUrl("/erp/impexp/export/index/orders_with_items/xml"))
                        .Field("dataSubset", "a:0:{}")
                        .Field("data", string.Empty);

            CreateMassFilter(fields, post);

            var stringData = post.Call();

            var ordersModel = ExportDocument.Parse(stringData);

            var result = ordersModel.Orders.Orders;

            foreach (var om in result)
            {
                om.ErpSystemId = Erp.Id;
            }

            return ordersModel.Orders.Orders;
        }

        private void ChangeOrderStatus(string orderId, string status)
        {
            EnsureSession();

            var result = m_client.Post(ActionUrl("/erp/orders/main/changeStatus"))
                   .Field("arf", m_csrfToken)
                   .Field("order_id", orderId)
                   .Field("status", status)
                   .Field("statusmail", /*statusmail ? "on" :*/ string.Empty)
                   .Call<DefaultResponse>();

            if (!result.Success)
            {
                throw new Exception(result.OriginalMessage);
            }
        }

        public void MarkOrderPaid(IPurchaseOrder po)
        {
            if (!m_config.EnableWriteOperations)
            {
                m_log.Error($"!!! Flox - MarkOrderPaid({po.OrderNumber}) - neaktivni operace");
                return;
            }

            ChangeOrderStatus(po.ErpOrderId, FloxOrderStatuses.Paid);
        }

        public IErpOrderModel LoadOrder(string orderNumber)
        {
            EnsureSession();

            var dlToken = ((long)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)).ToString();

            var fields = new Dictionary<string, string> { ["downloadToken"] = dlToken, ["order_num"] = orderNumber };

            var post = m_client.Post(ActionUrl("/erp/impexp/export/index/orders_with_items/xml"))
                        .Field("dataSubset", "a:0:{}")
                        .Field("data", string.Empty);

            CreateMassFilter(fields, post);

            var stringData = post.Call();

            var ordersModel = ExportDocument.Parse(stringData);

            var result = ordersModel.Orders.Orders;

            foreach (var om in result)
            {
                om.ErpSystemId = Erp.Id;
            }

            return ordersModel.Orders.Orders.FirstOrDefault(o => o.OrderNumber == orderNumber);
        }

        private void Login()
        {
            m_log.Info("Prihlasuji se k Floxu");
            
            var result =
                m_client.Post(ActionUrl("/admin/login/authenticate/"))
                    .Field("username", m_config.User)
                    .Field("password", m_config.Password)
                    .Call<DefaultResponse>();

            if (!result.Success)
            {
                m_log.Error($"Prihlaseni k Floxu se nezdarilo: \"{result.OriginalMessage}\"");
                throw new Exception(result.OriginalMessage);
            }
            
            var ordersPage = m_client.GetString(ActionUrl("/erp/main/orders"));

            var x = ordersPage.IndexOf("var CsrfToken=function()", StringComparison.InvariantCultureIgnoreCase);
            if (x < 0)
            {
                throw new Exception("No CsrfToken found");
            }

            var token = ordersPage.Substring(x);
            x = token.IndexOf('\'');
            token = token.Substring(x + 1);
            x = token.IndexOf('\'');
            m_csrfToken = token.Substring(0, x);

            m_lastAccess = DateTime.Now;
        }

        private string ActionUrl(string action)
        {
            return $"{m_config.Url}{action}";
        }

        private void EnsureSession()
        {
            try
            {

                if ((DateTime.Now - m_lastAccess).TotalMinutes > 1d)
                {
                    Login();
                }
                m_lastAccess = DateTime.Now;
            }
            catch (Exception ex)
            {
                m_log.Error("Proihlaseni k Floxu selhalo", ex);
                throw;
            }
        }

        public void Dispose()
        {
            m_client.Dispose();
        }

        private static void CreateMassFilter(Dictionary<string, string> parameters, IPostBuilder post)
        {
            var sb = new StringBuilder();

            sb.Append("a:").Append(parameters.Count).Append(":{");

            foreach (var kv in parameters)
            {
                RenderField(kv.Key, sb);
                RenderField(kv.Value, sb);
            }

            sb.Append("}");

            post.Field("massFilter", sb.ToString());

            foreach (var kv in parameters)
            {
                post.Field(kv.Key, kv.Value);
            }
        }

        private static void RenderField(string field, StringBuilder sb)
        {
            sb.Append("s:").Append(field.Length).Append(":\"").Append(field).Append("\";");
        }
    }
}
