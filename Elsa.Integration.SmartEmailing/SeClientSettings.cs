using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Mtecl.ApiClientBuilder;

namespace SmartEmailingApi.Client
{
    public class SeClientSettings : ApiClientFactorySettings
    {
        public SeClientSettings()
        {
            BaseUrl = "https://app.smartemailing.cz/";

            RequestSetup = (request, _d, _u, _m) =>
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    scheme: "Basic",
                    parameter: Convert.ToBase64String(Encoding.UTF8.GetBytes(User + ":" + Password))
                );
            };
        }

        public string User { get; set; }

        public string Password { get; set; }

    }
}
