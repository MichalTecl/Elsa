using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Mtecl.ApiClientBuilder.Proxy;

namespace Mtecl.ApiClientBuilder
{
    public class ApiClientFactory
    {
        public static T Get<T>(ApiClientFactorySettings settings)
        {
            var proxy = DispatchProxy.Create<T, ApiClientDispatchProxy>();

            (proxy as IHasSettings)?.Initialize(settings);

            return proxy;
        }
    }
}
