using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Elsa.App.PublicFiles.Impl
{
    public class PublicFiles : IPublicFilesHelper
    {
        private readonly ILog _log;
        private readonly PublicFilesConfig _config;

        public PublicFiles(ILog log, PublicFilesConfig config)
        {
            _log = log;
            _config = config;
        }

        public FileResult GetFile(string cid, string ftype)
        {
            if (!ValidateDirectory(cid) || !ValidateDirectory(ftype))
            {
                return new FileResult("error.txt", Encoding.ASCII.GetBytes("404"));
            }

            var path = $"C:\\Elsa\\PublicFiles\\{cid}\\{ftype}";

            var file = System.IO.Directory.GetFiles(path, "*.public").FirstOrDefault();
            if (file == null)
                return new FileResult("error.txt", Encoding.ASCII.GetBytes("404"));

            return new FileResult(Path.GetFileNameWithoutExtension(file), File.ReadAllBytes(file));
        }

        public void Write(string customerName, string fileType, string fileName, Action<StreamWriter> generate)
        {
            if (!ValidateDirectory(customerName))
                throw new ArgumentException("Invalid customer name");

            if(!ValidateDirectory(fileType))
                throw new ArgumentException("Invalid file type");

            var directory = $"C:\\Elsa\\PublicFiles\\{customerName}\\{fileType}";
            Directory.CreateDirectory(directory);

            var tempFile = Path.Combine(directory, $"{fileName}.temp");
            InRetryLoop(() => { 
                if(File.Exists(tempFile))
                    File.Delete(tempFile);
            });

            using(var writer = new StreamWriter(File.OpenWrite(tempFile), Encoding.UTF8))
            {
                generate(writer);
            }

            var bakFile = Path.Combine(directory, $"{fileName}.bak");
            InRetryLoop(() =>
            {
                if(File.Exists(bakFile))
                    File.Delete(bakFile);
            });

            InRetryLoop(() =>
            {
                if(File.Exists(Path.Combine(directory, $"{fileName}.public")))
                    File.Move(Path.Combine(directory, $"{fileName}.public"), bakFile);
            });

            InRetryLoop(() =>
            {
                File.Move(tempFile, Path.Combine(directory, $"{fileName}.public"));
            });

            PurgeCloudflareCache();
        }

        private bool ValidateDirectory(string inp)
        {
            if (string.IsNullOrWhiteSpace(inp) || inp.Length > 100)
                return false;

            return inp.ToLower().All(x => "abcdefghijklmnopqrstuvwxyz0123456789".Contains(x));
        }

        private void InRetryLoop(Action action)
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException)
                {
                    if (i == 4)
                    {
                        throw;
                    }

                    Thread.Sleep(100 * i);
                }
            }
        }

        private void PurgeCloudflareCache()
        {
            if (AppEnvironment.IsDev)
            {
                _log.Info("Skipping Cloudflare cache purge because application is running in DEV environment");
                return;
            }

            if (_config == null)
            {
                const string message = "PublicFilesConfig is not configured";
                _log.Error(message);
                throw new InvalidOperationException(message);
            }

            if (string.IsNullOrWhiteSpace(_config.CfZoneId) || string.IsNullOrWhiteSpace(_config.CfToken))
            {
                const string message = "Cloudflare purge is not configured. Missing CloudFlare.ZoneId or CloudFlare.ApiToken";
                _log.Error(message);
                throw new InvalidOperationException(message);
            }

            _log.Info($"Purging Cloudflare cache for zone '{_config.CfZoneId}'");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _config.CfToken);

                var response = httpClient.PostAsync(
                    $"https://api.cloudflare.com/client/v4/zones/{_config.CfZoneId}/purge_cache",
                    new StringContent("{\"purge_everything\":true}", Encoding.UTF8, "application/json"))
                    .GetAwaiter()
                    .GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    var responseText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    _log.Error($"Cloudflare cache purge failed with status {(int)response.StatusCode}: {responseText}");
                    throw new InvalidOperationException(
                        $"Cloudflare cache purge failed with status {(int)response.StatusCode}: {responseText}");
                }

                _log.Info($"Cloudflare cache purge finished for zone '{_config.CfZoneId}'");
            }
        }
    }
}
