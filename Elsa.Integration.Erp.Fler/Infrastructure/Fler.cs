using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Elsa.Integration.Erp.Fler.Model;

using Newtonsoft.Json;

namespace Elsa.Integration.Erp.Fler.Infrastructure
{
    public class Fler
    {
        private const string c_apiHashPrefix = "API1_SESS";
        private const string c_headerKey = "X-FLER-AUTHORIZATION";

        private const string c_post = "POST";
        private const string c_get = "GET";

        private SessionModel m_session;

        public virtual void LogIn(string user, string pwd)
        {
            m_session = Post<SessionModel>(c_post, "/api/rest/user/auth", new { username = user, pwd = pwd });
        }

        public virtual List<FlerOrderModel> LoadOrders()
        {
            return Post<List<FlerOrderModel>>(c_get, "/api/rest/seller/orders/list" /*, new { state = "NOVA" }*/ );
        }

        public RootObject LoadOrderDetail(string orderId)
        {
            return Post<RootObject>(c_get, $"/api/rest/seller/orders/detail/{orderId}" /*, new { state = "NOVA" }*/ );
        }


        public virtual void SetOrderPaid(string orderId)
        {
            var result = Post<FlerResponse>(c_post, "/api/rest/seller/orders/manage/paid", new { id_order = orderId });

            if (!string.IsNullOrWhiteSpace(result.error) || !string.IsNullOrWhiteSpace(result.error_number))
            {
                throw new InvalidOperationException($"Fler vrátil chybu: error='{result.error}', error_number='{result.error_number}'");
            }
        }

        private T Post<T>(string method, string requestPath, object o = null)
        {

            var request = (HttpWebRequest)WebRequest.Create($"https://www.fler.cz{requestPath}");

            var data = new byte[0];
            if (o != null)
            {
                var postData = JsonConvert.SerializeObject(o);
                data = Encoding.UTF8.GetBytes(postData);
            }

            request.Method = method;
            request.ContentLength = data.Length;

            SetSessionHeader(method, requestPath, request);

            if (o != null)
            {
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var responseString = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(responseString);
                }
            }
        }

        private void SetSessionHeader(string httpMethod, string httpPath, HttpWebRequest request)
        {
            if (m_session == null)
            {
                return;
            }

            var timestamp = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
            var sessionId = m_session.SessionId;
            var secretKey = m_session.SecretKey;

            var requestString = $"{httpMethod}\n{timestamp}\n{httpPath}";
            var requestStringHashed = HashHmac(requestString, secretKey);

            var rqsh64 = System.Convert.ToBase64String(Encoding.ASCII.GetBytes(requestStringHashed));

            var authString = $"{c_apiHashPrefix} {sessionId} {timestamp} {rqsh64}";

            request.Headers.Add(c_headerKey, authString);
        }

        private static string HashHmac(string message, string secret)
        {
            Encoding encoding = Encoding.UTF8;
            using (HMACSHA1 hmac = new HMACSHA1(encoding.GetBytes(secret)))
            {
                var msg = encoding.GetBytes(message);
                var hash = hmac.ComputeHash(msg);
                //return Convert.ToBase64String(hash);
                return BitConverter.ToString(hash).ToLower().Replace("-", string.Empty);
            }
        }

        private class FlerResponse : List<string>
        {
            public string error { get; set; }

            public string error_number { get; set; }
        }
    }
}
