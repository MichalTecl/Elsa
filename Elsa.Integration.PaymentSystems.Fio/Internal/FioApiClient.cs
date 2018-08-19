using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

using Newtonsoft.Json;

namespace Elsa.Integration.PaymentSystems.Fio.Internal
{
    public class FioApiClient 
    {
        private static readonly object s_dictionaryLock = new object();
        private static readonly Dictionary<string, DateTime> s_lastAccess = new Dictionary<string, DateTime>();

        public IEnumerable<AccountStatementModel.FioPayment> LoadPayments(string token, DateTime from, DateTime to)
        {
            DateTime lastAccess;
            lock (s_dictionaryLock)
            {
                if (!s_lastAccess.TryGetValue(token, out lastAccess))
                {
                    lastAccess = DateTime.MinValue;
                }
            }


            var sleepTime = 31000 - (DateTime.Now - lastAccess).TotalMilliseconds;

            if (sleepTime > 0)
            {
                Console.WriteLine($"Cekani na obnoveni FIO tokenu {sleepTime / 1000} sekund");
                Thread.Sleep((int)sleepTime);
            }

            var deser = new JsonSerializer();

            string url = $"https://www.fio.cz/ib_api/rest/periods/{token}/{@from:yyyy-MM-dd}/{to:yyyy-MM-dd}/transactions.json";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            PaymentsReportModel report;

            while(true)
            {
                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        lock (s_dictionaryLock)
                        {
                            s_lastAccess[token] = DateTime.Now;
                        }
                        
                        using (Stream stream = response.GetResponseStream())
                        using (var textReader = new StreamReader(stream))
                        using (JsonReader reader = new JsonTextReader(textReader))
                        {
                            report = deser.Deserialize<PaymentsReportModel>(reader);
                            break;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    Thread.Sleep(30000);
                }                
            }

            return report.AccountStatement.GetPayments().Where(p => p.Value >= 0);
        }
    }
}
