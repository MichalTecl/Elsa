using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Mtecl.ApiClientBuilder.Abstract;
using Mtecl.ApiClientBuilder.Helpers;

namespace Mtecl.ApiClientBuilder.Proxy
{
    internal class ApiClientDispatchProxy : DispatchProxy, IHasSettings
    {
        private static readonly MethodInfo TaskFromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult));

        private ApiClientFactorySettings _settings = null;
        
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (_settings == null)
                throw new InvalidOperationException("Missing initialization");

            if (targetMethod == null)
                return null;

            var resourceUrl = _settings.GetResourceUrl(targetMethod) ?? throw new ArgumentException("Cannot obtain resourcve Url");
            var httpMethod = _settings.GetHttpMethod(targetMethod);

            var paramlist = new Paramlist(targetMethod, args ?? Array.Empty<object>());

            resourceUrl = _settings.ModifyUrlByParams(httpMethod, new Uri(resourceUrl.ToString()), paramlist);

            var request = new HttpRequestMessage(httpMethod, resourceUrl);

            var payload = _settings.SetPayload(httpMethod, paramlist, resourceUrl, request);

            _settings.RequestSetup?.Invoke(request, resourceUrl, httpMethod, paramlist);

            _settings.PeepRequest?.Invoke(httpMethod, resourceUrl, payload);

            var returnType = targetMethod.ReturnType;
            var isTaskOfT = returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>);

            if (isTaskOfT)
                returnType = returnType.GetGenericArguments()[0]; 

            HttpClient client = null;
            try
            {
                client = _settings.GetHttpClient();

                var response = client.SendAsync(request).Result;

                var respStr = response.Content.ReadAsStringAsync().Result;

                var value = _settings.DeserializeResponse(respStr, returnType);

                (value as IInjectResponseData)?.InjectResponseData(respStr);

                _settings.PeepResult?.Invoke(value ?? "null");

                if (isTaskOfT)
                {
                    Task.FromResult<int>(123);
                    var fromResult = TaskFromResultMethod.MakeGenericMethod(returnType);
                    return fromResult.Invoke(null, new[] { value });
                }
                else
                {
                    return value;
                }
            }
            finally
            {
                if (client != null)
                    _settings.DisposeHttpClient(client);
            }
        }

        public void Initialize(ApiClientFactorySettings settings)
        {
            if (_settings != null)
                throw new InvalidOperationException("Proxy was already initialized");

            _settings = settings;
        }
    }
}
