using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Elsa.Common.Utils
{
    public static class SharedFilesUtil
    {
        private const int MUTEX_TIMEOUT_MS = 5000;
        private const int FILE_ACCESS_RETRY_COUNT = 8;
        private const int MAX_FILE_ACCESS_RETRY_DELAY_MS = 1000;
        private const int SHARING_VIOLATION_ERROR_CODE = 32;
        private const int LOCK_VIOLATION_ERROR_CODE = 33;

        public static void SetSharedValue(string key, string value) 
        {
            InMutex<object>(key, () => {
                RetryFileAccess(() => File.WriteAllText(GetSharedFileName(key), value, Encoding.UTF8));
                return null;
            });
        }

        public static string GetSharedValue(string key, string defaultValue) 
        {
            return InMutex<string>(key, () => { 
                var fn = GetSharedFileName(key);

                if (!File.Exists(fn))
                    return defaultValue;

                return RetryFileAccess(() => File.ReadAllText(fn, Encoding.UTF8));
            });
        }

        private static T InMutex<T>(string key, Func<T> fn) 
        {
            using (var mutex = new Mutex(false, $"ElsaSharedFileMutex_{key}")) 
            {
                var mutexAcquired = false;

                try
                {
                    try
                    {
                        mutexAcquired = mutex.WaitOne(MUTEX_TIMEOUT_MS);
                    }
                    catch (AbandonedMutexException)
                    {
                        // The previous owner terminated without releasing the mutex.
                        // The current thread owns it and can safely continue.
                        mutexAcquired = true;
                    }

                    if (!mutexAcquired)
                    {
                        throw new TimeoutException($"Waiting for exclusive access acquirance timed out. Attempted key={key}");
                    }

                    return fn();
                }
                finally
                {
                    if (mutexAcquired)
                    {
                        mutex.ReleaseMutex();
                    }
                } 
            }

        }

        private static void RetryFileAccess(Action action)
        {
            RetryFileAccess<object>(() =>
            {
                action();
                return null;
            });
        }

        private static T RetryFileAccess<T>(Func<T> action)
        {
            var delayMs = 50;

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return action();
                }
                catch (IOException ex) when (IsFileLocked(ex) && attempt < FILE_ACCESS_RETRY_COUNT)
                {
                    Thread.Sleep(delayMs);
                    delayMs = Math.Min(delayMs * 2, MAX_FILE_ACCESS_RETRY_DELAY_MS);
                }
            }
        }

        private static bool IsFileLocked(IOException exception)
        {
            var errorCode = exception.HResult & 0xFFFF;
            return errorCode == SHARING_VIOLATION_ERROR_CODE || errorCode == LOCK_VIOLATION_ERROR_CODE;
        }

        private static string GetSharedFileName(string key) 
        {
            var sharedDir = "C:\\Elsa\\InterprocSharedFiles";
            Directory.CreateDirectory(sharedDir);

            return Path.Combine(sharedDir, $"{key}.elsashared");
        }
    }
}
