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
        public IEnumerable<AccountStatementModel.FioPayment> LoadPayments(string token, DateTime from, DateTime to)
        {
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
                    using (Stream stream = response.GetResponseStream())
                    using (var textReader = new StreamReader(stream))
                    using (JsonReader reader = new JsonTextReader(textReader))
                    {
                        report = deser.Deserialize<PaymentsReportModel>(reader);
                        break;
                    }
                }
                catch(Exception ex)
                {
                    Thread.Sleep(1000);
                }                
            }

            return report.AccountStatement.GetPayments().Where(p => p.Value >= 0);
        }
    }
}
