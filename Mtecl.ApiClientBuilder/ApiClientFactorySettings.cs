using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Web;
using Mtecl.ApiClientBuilder.Abstract;
using Mtecl.ApiClientBuilder.Helpers;

namespace Mtecl.ApiClientBuilder
{
    public class ApiClientFactorySettings
    {
        public string BaseUrl { get; set; }
        public Action<HttpRequestMessage, Uri, HttpMethod, Paramlist> RequestSetup { get; set; }
        
        public Action<object> PeepResult { get; set; } 
        public Action<HttpMethod, Uri, string> PeepRequest { get; set; }

        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public virtual HttpMethod GetHttpMethod(MethodInfo method)
        {
            return GetAttribute<IHttpMethodProvider>(method)?.HttpMethod ??
                   throw new ArgumentException("No http method provider");
        }
        
        public virtual Uri GetResourceUrl(MethodInfo method)
        {
            var parts = new List<Uri>(2);

            if(!string.IsNullOrWhiteSpace(BaseUrl))
                parts.Add(new Uri(BaseUrl, UriKind.Absolute));

            var actionUrl = GetAttribute<IActionUrlProvider>(method)?.Url;
            if(!string.IsNullOrWhiteSpace(actionUrl))
                parts.Add(new Uri(actionUrl, UriKind.RelativeOrAbsolute));


            while (parts.Count > 1)
            {
                parts[parts.Count - 2] = new Uri(parts[parts.Count - 2], parts[parts.Count - 1]);
                parts.RemoveAt(parts.Count - 1);
            }

            return parts.FirstOrDefault();
        }

        public virtual HttpClient GetHttpClient()
        {
            var httpClientHandler = new HttpClientHandler
            {
                Credentials = new NetworkCredential("a@b.c", "???"),
                PreAuthenticate = true
            };

            return new HttpClient(httpClientHandler);
        }

        public virtual void DisposeHttpClient(HttpClient httpClient)
        {
            httpClient.Dispose();
        }

        protected virtual T GetAttribute<T>(MethodInfo method)
        {
            foreach(var attr in Attribute.GetCustomAttributes(method))
                if (attr is T found)
                    return found;

            return default(T);
        }
        
        public Uri ModifyUrlByParams(HttpMethod httpMethod, Uri uri, Paramlist paramlist)
        {
            var url = uri.ToString();

            paramlist.Visit((k, v) =>
            {
                var marker = $":{k}";
                if (url.Contains(marker))
                {
                    url = url.Replace(marker, v?.ToString() ?? string.Empty);
                    paramlist.Consume(k);
                }
            });

            var modified = new Uri(url);

            var query = new Dictionary<string, string>();

            paramlist.Visit((k, v) =>
            {
                var queryKey = paramlist.GetAttribute<IQueryStringParam>(k);
                if (queryKey != null)
                {
                    if (v != null)
                    {
                        if (v is IDictionary dictionaryValue)
                        {
                            foreach (var key in dictionaryValue.Keys)
                                query[key.ToString()] = dictionaryValue[key]?.ToString();
                        }
                        else
                        {
                            query[queryKey.Key ?? k] = v.ToString();
                        }
                    }

                    paramlist.Consume(k);
                }
            });

            if (query.Any())
            {
                var qstr = HttpUtility.ParseQueryString(modified.Query);
                foreach(var kv in query)
                    qstr[kv.Key] = kv.Value;

                var builder = new UriBuilder(modified)
                {
                    Query = qstr.ToString()
                };
                modified = builder.Uri;
            }

            return modified;
        }

        public string SetPayload(HttpMethod httpMethod, Paramlist paramlist, Uri resourceUrl, HttpRequestMessage request)
        {
            if (!paramlist.Any())
                return null;

            string payload;

            if (paramlist.Names.Count() == 1)
            {
                var val = paramlist.Consume(paramlist.Names.Single());
                payload = JsonSerializer.Serialize(val, JsonSerializerOptions);
            }
            else
            {
                var dct = new Dictionary<string, object>();

                paramlist.Visit((k, v) =>
                {
                    dct[k] = v;
                    paramlist.Consume(k);
                });

                payload = JsonSerializer.Serialize(dct, JsonSerializerOptions);
            }

            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            return payload;
        }

        public object DeserializeResponse(string respStr, Type returnType)
        {
            return JsonSerializer.Deserialize(respStr, returnType, JsonSerializerOptions);
        }
    }
}
