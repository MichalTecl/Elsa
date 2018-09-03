using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace Elsa.Common.Communication
{
    public class WebFormsClient : WebClient
    {
        private readonly CookieContainer m_container = new CookieContainer();

       
        

        public IPostBuilder Post(string url)
        {
            return new PostBuilder(this, url);
        }

        public string GetString(string url)
        {
            var bytes = DownloadData(url);

            return Encoding.UTF8.GetString(bytes);
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

            public PostBuilder(WebClient client, string url)
            {
                m_url = url;
                m_client = client;
            }

            public string Call()
            {
                m_client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                byte[] result = m_client.UploadValues(m_url, "POST", m_fields);

                return Encoding.UTF8.GetString(result);
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
