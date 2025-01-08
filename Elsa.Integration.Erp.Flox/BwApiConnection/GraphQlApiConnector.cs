using BwApiClient;
using BwApiClient.Model.Data;
using BwApiClient.Model.Inputs;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.Erp.Flox.BwApiConnection.Model;
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Integration.Erp.Flox.BwApiConnection
{
    internal class GraphQlApiConnector 
    {
        private static readonly ConcurrentDictionary<string, Mutex> _apiAccessMutexes = new ConcurrentDictionary<string, Mutex>();

        private readonly FloxClientConfig _config;
        private readonly ILog _log;

        public GraphQlApiConnector(FloxClientConfig config, ILog log)
        {
            _config = config;
            _log = log;                  
        }

        public IErpOrderModel LoadOrder(string orderNumber)
        {
            _log.Info($"Loading order {orderNumber}");
           
            return Map(CallApi(a => a.GetOrder(orderNumber)));
        }
                
        public List<IErpOrderModel> LoadOrders(DateTime changedFrom, int? orderStatusId = null)
        {
            _log.Info($"Loading orders changed after {changedFrom}");

            var result = new List<IErpOrderModel>();

            int? cursor = null;

            do
            {
                _log.Info($"Requesting page of orders, cursor={cursor}");

                PaginatedList<Order> page = null;

                var succeeded = false;
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        if (attempt > 0)
                        {
                            var delay = attempt * 1000;
                            _log.Info($"Got an error in attempt={attempt}/5; waiting {delay}ms");
                            Thread.Sleep(delay);
                            
                            _log.Info($"Trying attempt {attempt + 1}");
                        }

                        page = CallApi(a => a.GetOrders(changedFrom, orderStatusId: orderStatusId, listOffset: cursor));
                        cursor = page.pageInfo.nextCursor;

                        _log.Info($"Received {page.data.Count} of orders");

                        if (cursor != null)
                        {
                            _log.Info($"Received next cursor {cursor}. Waiting 300ms to not overload BW server");
                            Thread.Sleep(300);
                        }

                        succeeded = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _log.Info($"BW 'soft' error {ex}");
                    }
                }

                if (!succeeded)
                {
                    _log.Error("5 attempts to get page of orders failed");
                    throw new Exception("Cannot download orders");
                }

                foreach (var o in page.data)
                    result.Add(Map(o));

                _log.Info("Orders mapped");

            } while (cursor != null);

            _log.Info($"Completed loading of {result.Count} orders");

            return result;
        }
                
        public void MakeOrderSent(IPurchaseOrder po)
        {
            throw new NotImplementedException();
        }

        public void MarkOrderPaid(IPurchaseOrder po)
        {
            throw new NotImplementedException();
        }

        internal string LoadOrderInternalNote(string orderNumber)
        {
            var order = CallApi(c => c.GetShallowOrder(orderNumber));

            return order?.internal_note;
        }

        private T CallApi<T>(Func<BwClient, Task<T>> op)
        {
            if (!_config.PreferApi)
                throw new InvalidOperationException("Use of BW API is forbidden");

            var rq = "?";
            var resp = "?";
            var url = _config.ApiUrl;

            var mutexName = $"Global\\BwApiExclusive{_config.ApiUrl}:{StringUtil.GetHash(_config.ApiKey)}";

            var mutex = _apiAccessMutexes.GetOrAdd(mutexName, k => new Mutex(false, mutexName));

            bool lockTaken = false;
            try
            {
                _log.Info($"Trying to acquire mutex '{mutexName}'");
                lockTaken = mutex.WaitOne(2 * 60 * 1000, false); // 2 minutes
                if (!lockTaken)
                {
                    _log.Error($"Failed to acquire mutex '{mutexName}'");
                    throw new TimeoutException("Časový limit pro přístup k Byznysweb API vypršel, operaci nelze dokončit");
                }
                _log.Info($"Acquired mutex '{mutexName}'");

                using (var http = new HttpClient())
                {
                    T result;

                    try
                    {
                        var client = new BwClient(url, _config.ApiKey, () => http);

                        client.Options.RawRequestPeek = (r) => rq = r;
                        client.Options.RawResponsePeek = (r) => resp = r;

                        var call = Task.Run<T>(async () => await op(client));

                        result = call.Result;
                    }
                    finally
                    {
                        _log.SaveRequestProtocol("POST", _config.Url, rq, resp, LogExtensions.Extras.PrettifyJsonReponse);
                    }

                    return result;            
                }
            }
            catch (Exception e)
            {
                _log.Error("BW API call failed", e);
                _log.Error($"URL={url}");
                _log.Error($"Request:{rq}");
                _log.Error($"Response:{resp}");

                throw;
            }
            finally
            {
                if (lockTaken)
                {                    
                    mutex.ReleaseMutex();
                    _log.Info($"Released mutex '{mutexName}'");
                }
            }
        }
               
        private IErpOrderModel Map(Order order)
        {
            if (order == null)
                return null;

            return new ApiOrderModel(order);
        }
    }
}
