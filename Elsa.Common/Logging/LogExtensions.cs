using Elsa.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Logging
{
    public static class LogExtensions
    {
        private static readonly object s_protoLock = new object();

        public static string SaveRequestProtocol(this ILog log, string method, string url, IDictionary<string, object> sent, string received) 
        {
            var sentSb = new StringBuilder();
            
            if (sent != null)
                foreach(var d in sent)
                {
                    var v = d.Value;                    
                    sentSb.AppendLine($"{d.Key}:{(v?.ToString()?.Length ?? 0)}B");
                }

            return SaveRequestProtocol(log, method, url, sentSb.ToString(), received);
        }

        public static string SaveRequestProtocol(this ILog log, string method, string url, string sent, string received)
        {
            var fn = $"{DateTime.Now:yyyyMMdd HHmmss} {method} {StringUtil.SanitizeFileName(url, '.')}";

            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(sent))
            {
                sb.AppendLine("SENT:");
                sb.AppendLine(sent);
                sb.AppendLine();
            }

            sb.AppendLine("RECEIVED:");
            sb.AppendLine(received);

            

            lock (s_protoLock)
            {
                var dir = Directory.CreateDirectory("c:\\Elsa\\Log\\Communication");

                var rawPath = Path.Combine(dir.FullName, fn);

                var path = rawPath;
                int i = 0;
                while (File.Exists(path))
                {
                    i++;
                    path = StringUtil.AddNumberToFileName(rawPath, i);
                }

                File.WriteAllText(path, sb.ToString());

                log.Info($"Communication protocol saved as {path}");

                DirectorySizeKeeper.KeepSize(dir.FullName, (int)5e+8, (int)2e+8, log);

                return path;
            }
        }
    }
}
