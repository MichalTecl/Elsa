using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Mtecl.ApiClientBuilder;

namespace SmartEmailingApi.Client
{
    public class SmartEmailingApiFactory : ISmartEmailingApiFactory
    {
        private readonly SeClientSettings _settings;

        public SmartEmailingApiFactory(SeClientSettings settings)
        {
            _settings = settings;
        }

        public T Get<T>()
        {
            return ApiClientFactory.Get<T>(_settings);
        }
    }
}
