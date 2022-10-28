using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public static class SharedFilesUtil
    {
        public static void SetSharedValue(string key, string value) 
        {
            InMutex<object>(key, () => {
                File.WriteAllText(GetSharedFileName(key), value, Encoding.UTF8);
                return null;
            });
        }

        public static string GetSharedValue(string key, string defaultValue) 
        {
            return InMutex<string>(key, () => { 
                var fn = GetSharedFileName(key);

                if (!File.Exists(fn))
                    return defaultValue;

                return File.ReadAllText(fn, Encoding.UTF8);
            });
        }

        private static T InMutex<T>(string key, Func<T> fn) 
        {
            using (var mutex = new Mutex(false, $"ElsaSharedFileMutex_{key}")) 
            {
                try
                {
                    if (!mutex.WaitOne(5000)) 
                    {
                        throw new TimeoutException($"Waiting for exclusive access acquirance timed out. Attempted key={key}");
                    }

                    return fn();
                }
                finally
                {
                    mutex.ReleaseMutex();
                } 
            }

        }

        private static string GetSharedFileName(string key) 
        {
            var sharedDir = "C:\\Elsa\\InterprocSharedFiles";
            Directory.CreateDirectory(sharedDir);

            return Path.Combine(sharedDir, $"{key}.elsashared");
        }
    }
}
