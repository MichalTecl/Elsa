using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Newtonsoft.Json;

namespace Elsa.Common.Communication
{
    public class WebFormsClient : WebClient
    {
        private readonly CookieContainer m_container = new CookieContainer();


        private readonly ILog m_log;

        public WebFormsClient(ILog log)
        {
            m_log = log;
        }

        public IPostBuilder Post(string url)
        {
            return new PostBuilder(this, url, m_log);
        }

        public string GetString(string url)
        {
            var bytes = DownloadData(url);

            var strResponse = Encoding.UTF8.GetString(bytes);

            m_log.SaveRequestProtocol("GET", url, string.Empty, strResponse);

            return strResponse;
        }

        public T Get<T>(string url)
        {
            var strResponse = GetString(url);

            var response = JsonConvert.DeserializeObject<T>(strResponse);

            var parsed = (response as IParsedResponse);
            if (parsed != null)
            {
                parsed.OriginalMessage = strResponse;
            }

            return response;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest webRequest = request as HttpWebRequest;
            if (webRequest != null)
            {
                webRequest.CookieContainer = m_container;
            }

            request.Timeout = 1000 * 60 * 10;
            return request;
        }
                
        private sealed class PostBuilder : IPostBuilder
        {
            private readonly string m_url;
            private readonly WebClient m_client;
            private readonly NameValueCollection m_fields = new NameValueCollection();
            private readonly ILog m_log;

            public PostBuilder(WebClient client, string url, ILog log)
            {
                m_url = url;
                m_client = client;
                m_log = log;
            }

            public string Call()
            {
                m_client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                byte[] result = m_client.UploadValues(m_url, "POST", m_fields);

                var strResult = Encoding.UTF8.GetString(result);

                m_log.SaveRequestProtocol("POST", m_url, m_fields.AllKeys.ToDictionary(k => k, k => (object)m_fields.Get(k)), strResult);

                return strResult;
            }

            public T Call<T>()
            {
                var strResponse = Call();

                var response = JsonConvert.DeserializeObject<T>(strResponse);

                var parsed = (response as IParsedResponse);
                if (parsed != null)
                {
                    parsed.OriginalMessage = strResponse;
                }

                return response;
            }

            public IPostBuilder Field(string name, string value)
            {
                m_fields.Add(name, value);

                return this;
            }
        }
    }
}
