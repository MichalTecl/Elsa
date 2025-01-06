using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Crm;
using Elsa.Commerce.Core.Model;
using Elsa.Common.Communication;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.Erp.Flox.BwApiConnection;
using Elsa.Integration.Erp.Flox.Protocol;
using Elsa.Integration.Erp.Flox.Protocol.CustomerModel;
using Elsa.Integration.Erp.Flox.Protocol.OrderModel;

namespace Elsa.Integration.Erp.Flox
{
    public class FloxClient : IErpClient, IDisposable
    {
        private readonly ILog _log;
        private readonly FloxClientConfig _config;
        private readonly IOrderStatusMappingRepository _statusMappingRepository;
        private string _csrfToken;
        private DateTime _lastAccess = DateTime.MinValue;
        private readonly WebFormsClient _client;
        private readonly ICustomerRepository _customerRepository;
        private readonly GraphQlApiConnector _apiConnector;

        public FloxClient(
            FloxClientConfig config,
            FloxDataMapper mapper,
            ILog log,
            IOrderStatusMappingRepository statusMappingRepository, ICustomerRepository customerRepository)
        {
            _config = config;
            Mapper = mapper;
            _log = log;
            _statusMappingRepository = statusMappingRepository;
            _client = new WebFormsClient(_log);
            _customerRepository = customerRepository;

            _apiConnector = new GraphQlApiConnector(config, log);
        }

        public IErp Erp { get; set; }

        public IErpDataMapper Mapper { get; }

        public IErpCommonSettings CommonSettings => _config;
               
        public IEnumerable<IErpOrderModel> LoadPaidOrders(DateTime from, DateTime to)
        {
            var mappings = _statusMappingRepository.GetMappings(Erp.Id);

            foreach (var m in mappings)
            {
                if (OrderStatus.IsPaid(m.Value.OrderStatusId))
                {
                    foreach (var order in LoadOrders(from, to, null, m.Key))
                    {
                        yield return order;
                    }
                }

            }
        }

        private void ChangeOrderStatus(string orderId, string status)
        {
            if (!_config.EnableWriteOperations)
            {
                _log.Error($"!!! Flox - ChangeOrderStatus({orderId}) - neaktivni operace");
                return;
            }

            EnsureSession();

            var result =
                _client.Post(ActionUrl("/erp/orders/main/changeStatus"))
                    .Field("arf", _csrfToken)
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
            ChangeOrderStatus(po.ErpOrderId, FloxOrderStatuses.Paid);
        }

        public IErpOrderModel LoadOrder(string orderNumber)
        {
            if (_config.PreferApi)
            {
                try
                {
                    var loaded = _apiConnector.LoadOrder(orderNumber);
                    loaded.ErpSystemId = Erp.Id;

                    return loaded;
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to load order ({orderNumber}) using API. Activating fallback to 'webform' mode", ex);
                }
            }

            return LoadOrderWithoutApi(orderNumber);
        }

        private IErpOrderModel LoadOrderWithoutApi(string orderNumber)
        {
            EnsureSession();

            var dlToken = ((long)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)).ToString();

            var fields = new Dictionary<string, string> { ["downloadToken"] = dlToken, ["order_num"] = orderNumber };

            var post =
                _client.Post(ActionUrl("/erp/impexp/export/index/orders_with_items/xml"))
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

        public void MakeOrderSent(IPurchaseOrder po)
        {
            _log.Info($"Zacinam nastavovat objednavku {po.OrderNumber} jako odeslanou");

            if (!_config.EnableWriteOperations)
            {
                _log.Error($"!!! Flox - MarkOrderPaid({po.OrderNumber}) - neaktivni operace");
                return;
            }
            
            try
            {
                GenerateInvoice(po.OrderNumber);
                SendInvoiceToCustomer(po.ErpOrderId);
                ChangeOrderStatus(po.ErpOrderId, FloxOrderStatuses.Completed);
            }
            catch (Exception ex)
            {
                throw new Exception($"Selhal pokus o zpracovani objednavky {po.OrderNumber}. Objednavku je treba dokoncit ve Floxu. Chyba: {ex.Message}", ex);
            }
        }

        public IEnumerable<IErpCustomerModel> LoadCustomers()
        {
            EnsureSession();

            _log.Info("Starting 'Persons' export");
            
            var url = ActionUrl("/erp/impexp/export/index/persons/xml");
            
            var exportString =
                _client.Post(url)
                    .Field(
                        "dataSubset",
                        "a:9:{s:10:\"xcol_email\";s:2:\"on\";s:9:\"xcol_name\";s:2:\"on\";s:12:\"xcol_surname\";s:2:\"on\";s:11:\"xcol_active\";s:2:\"on\";s:15:\"xcol_newsletter\";s:2:\"on\";s:17:\"xcol_address_name\";s:2:\"on\";s:20:\"xcol_address_surname\";s:2:\"on\";s:18:\"xcol_address_phone\";s:2:\"on\";s:11:\"xcol_groups\";s:2:\"on\";}")
                    .Field("data", string.Empty)
                    .Field("massFilter", string.Empty)
                    .Field("downloadToken", CalcDownloadToken())
                    .Call();
            
            var result = new List<IErpCustomerModel>();

            var parsed = PersonsDoc.Parse(exportString, false);

            var cgIndex = _customerRepository.GetCustomerGroupTypes();
                        
            result.AddRange(parsed.Select(i => new ErpPersonModel(i, cgIndex)));

            _log.Info($"Received {result.Count} of 'Persons'");

            _log.Info("Requesting 'Companies' export");
            exportString =
                _client.Post(ActionUrl("/erp/impexp/export/index/companies/xml"))
                    .Field(
                        "dataSubset",
                        "a:18:{s:15:\"xcol_company_id\";s:2:\"on\";s:9:\"xcol_name\";s:2:\"on\";s:11:\"xcol_vat_id\";s:2:\"on\";s:12:\"xcol_website\";s:2:\"on\";s:10:\"xcol_email\";s:2:\"on\";s:17:\"xcol_main_user_id\";s:2:\"on\";s:25:\"xcol_address_company_name\";s:2:\"on\";s:19:\"xcol_address_street\";s:2:\"on\";s:31:\"xcol_address_descriptive_number\";s:2:\"on\";s:31:\"xcol_address_orientation_number\";s:2:\"on\";s:17:\"xcol_address_city\";s:2:\"on\";s:16:\"xcol_address_zip\";s:2:\"on\";s:18:\"xcol_address_state\";s:2:\"on\";s:20:\"xcol_address_country\";s:2:\"on\";s:18:\"xcol_address_phone\";s:2:\"on\";s:11:\"xcol_groups\";s:2:\"on\";s:13:\"xcol_salesrep\";s:2:\"on\";s:14:\"xtab_addresses\";s:2:\"on\";}")
                    .Field("data", string.Empty)
                    .Field("massFilter", string.Empty)
                    .Field("downloadToken", CalcDownloadToken())
                    .Call();

            exportString = exportString.Replace("<companies>", "<persons>").Replace("</companies>", "</persons>");

            parsed = PersonsDoc.Parse(exportString, true);
            result.AddRange(parsed.Select(i => new ErpPersonModel(i, cgIndex)));

            _log.Info($"Collected {result.Count} of Persons + Companies records");

            _log.Info("Requesting newsletter subscribers export");
            var newsletterReceivers = LoadNewsletterReceivers().ToList();

            _log.Info($"Received {newsletterReceivers.Count} of newsletter subscribers");

            foreach(var subscriber in newsletterReceivers)
            {
                var customer = result.FirstOrDefault(r => r.Email.Equals(subscriber, StringComparison.InvariantCultureIgnoreCase));
                
                if (customer != null)
                {
                    if (!customer.IsNewsletterSubscriber)
                    {
                        customer.IsNewsletterSubscriber = true;
                        _log.Info($"Setting existing customer {subscriber} to be Newsletter subscriber");
                    }
                }
                else
                {                    
                    customer = new ErpPersonModel(new PersonModel 
                    {
                        Email = subscriber,
                        Active = 1,
                        Newsletter = 1
                    }, null);

                    result.Add(customer);
                }
            }

            return result;
        }

        public string GetPackingReferenceNumber(IPurchaseOrder po)
        {
            return po.VarSymbol;
        }

        private void GenerateInvoice(string orderNum)
        {
            EnsureSession();
            
            var timeStamp = ((long)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)).ToString();
            var url = ActionUrl($"/erp/orders/invoices/finalize/{orderNum}?arf={_csrfToken}&_dc={timeStamp}");
            _log.Info($"Incializuji generovani faktury ve Floxu: {url}");

            var result = _client.Get<DefaultResponse>(url);
            if (!result.Success)
            {
                _log.Error($"Generovani faktury selhalo. Request={url}, Response={result.OriginalMessage}");
                throw new Exception(result.OriginalMessage);
            }
            
            _log.Info($"Generovani fatktury OK OrderNum={orderNum}");
        }

        public void SendInvoiceToCustomer(string orderId)
        {
            EnsureSession();

            var timeStamp = ((long)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)).ToString();
            var url = ActionUrl($"/erp/orders/invoices/sendEmail/{orderId}?arf={_csrfToken}&_dc={timeStamp}");
            _log.Info($"Posilam pozadavek na odeslani e-mailu klientovi: {url}");

            var result = _client.Get<DefaultResponse>(url);
            if (!result.Success)
            {
                _log.Error($"Pozadavek na odeslani emailu klientovi selhal. Request={url}, Response={result.OriginalMessage}");
                throw new Exception(result.OriginalMessage);
            }

            _log.Info($"Odesilani emailu OK OrderId={orderId}");
        }

        private void Login()
        {
            _log.Info("Prihlasuji se k Floxu");
            
            var result =
                _client.Post(ActionUrl("/admin/login/authenticate/"))
                    .Field("username", _config.User)
                    .Field("password", _config.Password)
                    .Call<DefaultResponse>();

            if (!result.Success)
            {
                _log.Error($"Prihlaseni k Floxu se nezdarilo: \"{result.OriginalMessage}\"");
                throw new Exception(result.OriginalMessage);
            }
            
            var ordersPage = _client.GetString(ActionUrl("/erp/main/orders"));

            var x = ordersPage.IndexOf("var CsrfToken=function()", StringComparison.InvariantCultureIgnoreCase);
            if (x < 0)
            {
                throw new Exception("No CsrfToken found");
            }

            var token = ordersPage.Substring(x);
            x = token.IndexOf('\'');
            token = token.Substring(x + 1);
            x = token.IndexOf('\'');
            _csrfToken = token.Substring(0, x);

            _lastAccess = DateTime.Now;
        }

        private string ActionUrl(string action)
        {
            return $"{_config.Url}{action}";
        }

        private void EnsureSession()
        {
            try
            {

                if ((DateTime.Now - _lastAccess).TotalMinutes > 1d)
                {
                    Login();
                }
                _lastAccess = DateTime.Now;
            }
            catch (Exception ex)
            {
                _log.Error("Prihlaseni k Floxu selhalo", ex);
                throw;
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private IEnumerable<IErpOrderModel> LoadOrders(DateTime from, DateTime? to, DateTime? changedAfter, string status)
        {
            if (_config.PreferApi)
            {
                int? statusId = null;

                if (!string.IsNullOrEmpty(status))
                {
                    statusId = int.Parse(status);
                }

                var received = _apiConnector.LoadOrders(changedAfter ?? from, statusId);
                return PostMap(received);
            }
            
            _log.Info($"Zacinam stahovani objednavek od={from}, do={to}, status={status}");
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

            if (status != null)
            {
                fields["status"] = status;
            }

            fields["downloadToken"] = dlToken;

            var post = _client.Post(ActionUrl("/erp/impexp/export/index/orders_with_items/xml"))
                        .Field("dataSubset", "a:0:{}")
                        .Field("data", string.Empty);

            CreateMassFilter(fields, post);

            var stringData = post.Call();

            var ordersModel = ExportDocument.Parse(stringData);
                        
            return PostMap(ordersModel.Orders.Orders);
        }

        private IEnumerable<IErpOrderModel> PostMap(IEnumerable<IErpOrderModel> orders)
        {
            foreach(var om in orders)
            {
                om.ErpSystemId = Erp.Id;
                ValidateOrderData(om);

                yield return om;
            }
        }

        private void ValidateOrderData(IErpOrderModel om)
        {
            // All items must be unique
            var uniqueItems = new HashSet<string>();
            foreach(var i in om.LineItems)
            {
                if (!uniqueItems.Add(i.ProductName))
                {
                    _log.Error($"Order item duplicity detected: OrderNr={om.OrderNumber} Item={i.ProductName}");
                }
            }
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

        private static string CalcDownloadToken()
        {
            return ((long)((DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds)).ToString();
        }

        private List<string> LoadNewsletterReceivers()
        {
            var exportString =
                _client.Post(ActionUrl("/erp/impexp/export/index/newsletterReceivers/xml"))
                    .Field(
                        "dataSubset",
                        "a:0:{}")
                    .Field("data", string.Empty)
                    .Field("massFilter", string.Empty)
                    .Field("downloadToken", CalcDownloadToken())
                    .Call();

            var parsed = NewsletterSubscriptionsModel.Parse(exportString);

            return parsed.Select(i => i.Email).ToList();
        }

        public IEnumerable<IErpOrderModel> LoadOrdersSnapshot(DateTime from, DateTime? to = null)
        {
            return LoadOrders(from, to, null, null);
        }

        public IEnumerable<IErpOrderModel> LoadOrdersIncremental(DateTime changedAfter)
        {
            return LoadOrders(changedAfter, null, changedAfter, null);
        }

        public string LoadOrderInternalNote(string orderNumber)
        {
            /*
                         
            Waiting for  Byznysweb fix

            return LoadOrder(orderNumber)?.InternalNote;
            */

            return LoadOrderWithoutApi(orderNumber)?.InternalNote;
        }
    }
}
