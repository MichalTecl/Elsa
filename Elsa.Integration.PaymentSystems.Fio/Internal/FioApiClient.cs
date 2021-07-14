using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Elsa.Common.Logging;
using Newtonsoft.Json;

namespace Elsa.Integration.PaymentSystems.Fio.Internal
{
    public class FioApiClient 
    {
        private static readonly object s_dictionaryLock = new object();
        private static readonly Dictionary<string, DateTime> s_lastAccess = new Dictionary<string, DateTime>();

        private readonly ILog m_log;

        public FioApiClient(ILog log)
        {
            m_log = log;
        }

        public IEnumerable<AccountStatementModel.FioPayment> LoadPayments(string token, DateTime from, DateTime to)
        {
            m_log.Info($"Obtaining fio last access time");
            DateTime lastAccess;
            lock (s_dictionaryLock)
            {
                m_log.Info($"Entered the lock");
                if (!s_lastAccess.TryGetValue(token, out lastAccess))
                {
                    m_log.Info($"Last access time unknown");
                    lastAccess = DateTime.MinValue;
                }
            }

            m_log.Info($"fio last access time = {lastAccess}");

            var sleepTime = 31000 - (DateTime.Now - lastAccess).TotalMilliseconds;

            if (sleepTime > 0)
            {
                m_log.Info($"Cekani na obnoveni FIO tokenu {sleepTime / 1000} sekund");
                Thread.Sleep((int)sleepTime);
            }

            var deser = new JsonSerializer();

            var url = $"https://www.fio.cz/ib_api/rest/periods/{token}/{@from:yyyy-MM-dd}/{to:yyyy-MM-dd}/transactions.json";

            const int attemptsCount = 4;
            PaymentsReportModel report = null;
            for (var retryCount = 0; retryCount <= attemptsCount; retryCount++)
            {
                try
                {
                    var request = (HttpWebRequest) WebRequest.Create(url);
                    request.Timeout = (int) TimeSpan.FromMinutes(1).TotalMilliseconds;
                    request.AutomaticDecompression = DecompressionMethods.GZip;

                    m_log.Info($"GET {url}:");
                    using (var response = (HttpWebResponse) request.GetResponse())
                    {
                        m_log.Info($"Received response {response.ContentType}");
                        m_log.Info($"Attempting to enter the lock to store last access time:");
                        lock (s_dictionaryLock)
                        {
                            s_lastAccess[token] = DateTime.Now;
                            m_log.Info(
                                $"Last access time stored; The dictionary contains {s_lastAccess.Count} token(s)");
                        }

                        m_log.Info($"Reading the response");
                        using (var stream = response.GetResponseStream())
                        using (var textReader = new StreamReader(stream))
                        using (JsonReader reader = new JsonTextReader(textReader))
                        {
                            report = deser.Deserialize<PaymentsReportModel>(reader);
                            m_log.Info(
                                $"Response properly deserialized. Contains {report.TransactionsCount} of transactions, AccountStatement.Info = {report.AccountStatement?.Info}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    report = null;
                    m_log.Info($"Attempt {retryCount} to download payments failed: {ex}");

                    if (attemptsCount == retryCount)
                    {
                        break;
                    }
                    
                    Thread.Sleep(30000);
                }
            }

            if (report == null)
            {
                m_log.Error("All attempts to download payments failed");
                throw new InvalidOperationException("Cannot download payments");
            }

            return report?.AccountStatement.GetPayments().Where(p => p.Value >= 0);
        }
    }
}
