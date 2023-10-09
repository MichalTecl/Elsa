using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Logging;
using Elsa.Integration.Crm.Raynet.Model;
using Elsa.Integration.Crm.Raynet.Model.JsonCustomizations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elsa.Integration.Crm.Raynet
{
    public class RnProtocol
    {
        private readonly ILog _log;

        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter> { new IdContainerResolver() }
        };

        private readonly string _userName;
        private readonly string _apiKey;
        private readonly string _instanceName;

        public RnProtocol(RaynetClientConfig cfg, ILog log):this(cfg.UserName, cfg.ApiKey, cfg.InstanceName, log) { }

        private RnProtocol(string userName, string apiKey, string instanceName, ILog log)
        {
            _userName = userName;
            _apiKey = apiKey;
            _instanceName = instanceName;
            _log = log;
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient(new HttpClientHandler { SslProtocols = SslProtocols.Tls12 });
                        
            ServicePointManager.Expect100Continue = true;
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string authValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_userName}:{_apiKey}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            client.DefaultRequestHeaders.Add("X-Instance-Name", _instanceName);

            return client;
        }

        public TResponse Call<TResponse>(HttpMethod method, string url, object query = null, object payload = null) where TResponse : RnResponse
        {
            string queryStr = MakeQuery(query);
            if (!string.IsNullOrEmpty(queryStr))
            {
                var separator = url.Contains("?") ? "&" : "?";
                url = $"{url}{separator}{queryStr}";
            }

            try
            {
                string payloadJson = null;
                
                using (var request = new HttpRequestMessage(method, url))
                {
                    if (payload != null)
                    {
                        payloadJson = ToJson(payload);

                        Console.WriteLine($"Payload: {payloadJson}");

                        request.Content = new StringContent(payloadJson, System.Text.Encoding.UTF8,
                            "application/json");
                    }

                    using (var client = GetClient())
                    {
                        using (var response = client.SendAsync(request).GetAwaiter().GetResult())
                        {
                            var responseJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                            if (!response.IsSuccessStatusCode)
                            {
                                var errMessage = $"Error: {response.StatusCode} - {response.ReasonPhrase}";
                                var derr = FromJson<ErrorResponse>(responseJson);
                                if (derr != null)
                                {
                                    errMessage = derr.Message;
                                }

                                _log.SaveRequestProtocol($"ERROR {method.Method}", url, payloadJson ?? "", responseJson ?? "");

                                throw new RaynetException(errMessage);
                            }

                            Console.WriteLine($"RESPONSE: {responseJson}");

                            try
                            {

                                var deserialized = FromJson<TResponse>(responseJson) ?? default(TResponse);

                                if (deserialized != null)
                                    deserialized.OriginalJson = responseJson;

                                _log.SaveRequestProtocol(method.Method, url, payloadJson ?? "", responseJson ?? "");

                                return deserialized;
                            }
                            catch(Exception ex) 
                            {                                
                                _log.SaveRequestProtocol($"ERROR {method.Method}", url, payloadJson ?? "", ex.Message + ":  " + responseJson );
                                throw;
                            }
                        }
                    }                    
                }
            }
            finally
            {
                Console.WriteLine("_______________________\n");
            }
        }

        private static string MakeQuery(object query)
        {
            if (query == null)
                return string.Empty;
            
            IEnumerable<Tuple<string, string>> generate()
            {
                foreach (var p in query.GetType().GetProperties())
                {
                    var val = p.GetValue(query)?.ToString();
                    if (val == null)
                        continue;

                    yield return new Tuple<string, string>(p.Name, val);
                }
            }

            return string.Join("&", generate().Select(g => $"{g.Item1}={g.Item2}"));
        }

        private static string ToJson(object model)
        {
            return JsonConvert.SerializeObject(model, _jsonSerializerSettings);
        }

        private static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        }
    }
}
