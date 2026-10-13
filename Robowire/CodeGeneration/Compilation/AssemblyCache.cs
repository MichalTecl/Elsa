using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace CodeGeneration.Compilation
{
    /// <summary>
    /// Disk cache of compiled assemblies in %TEMP%\RobowireContainers\{application}\{key}.dll
    /// The key must identify everything the assembly was compiled from.
    /// </summary>
    public static class AssemblyCache
    {
        private const string CACHE_FOLDER_NAME = "RobowireContainers";
        private const int MAX_CACHED_ASSEMBLIES_PER_APPLICATION = 10;

        private static readonly TimeSpan _maxUnusedCachedAssemblyAge = TimeSpan.FromDays(7);
        private static readonly TimeSpan _abandonedTempFileAge = TimeSpan.FromHours(1);

        public static bool Enabled { get; set; } = true;

        public static Assembly TryLoad(string cacheKey)
        {
            if (!Enabled || string.IsNullOrEmpty(cacheKey))
            {
                return null;
            }

            byte[] assemblyBytes;
            string cacheFile;

            try
            {
                cacheFile = GetCacheFilePath(cacheKey);

                if (!File.Exists(cacheFile))
                {
                    return null;
                }

                assemblyBytes = File.ReadAllBytes(cacheFile);
            }
            catch (Exception)
            {
                return null;
            }

            Assembly assembly;

            try
            {
                assembly = Assembly.Load(assemblyBytes);
            }
            catch (Exception)
            {
                TryDeleteFile(cacheFile);
                return null;
            }

            try
            {
                // Marks the file as used, so the cleanup keeps it.
                File.SetLastWriteTimeUtc(cacheFile, DateTime.UtcNow);
            }
            catch (Exception)
            {
                // Not important
            }

            return assembly;
        }

        public static void TrySave(string cacheKey, byte[] assemblyBytes)
        {
            if (!Enabled || string.IsNullOrEmpty(cacheKey))
            {
                return;
            }

            string cacheFile;

            try
            {
                cacheFile = GetCacheFilePath(cacheKey);
            }
            catch (Exception)
            {
                return;
            }

            var directory = Path.GetDirectoryName(cacheFile);
            var tempFile = $"{cacheFile}.{Guid.NewGuid():N}.tmp";

            try
            {
                Directory.CreateDirectory(directory);

                // Written under a temporary name and moved, so another process never reads a partially written file.
                File.WriteAllBytes(tempFile, assemblyBytes);

                if (!File.Exists(cacheFile))
                {
                    File.Move(tempFile, cacheFile);
                }
            }
            catch (Exception)
            {
                // The cache is optional; another process could have saved the same file in the meantime.
            }
            finally
            {
                TryDeleteFile(tempFile);
            }

            TryCleanUp(directory, Path.GetFileName(cacheFile));
        }

        public static void TryRemove(string cacheKey)
        {
            try
            {
                TryDeleteFile(GetCacheFilePath(cacheKey));
            }
            catch (Exception)
            {
                // Not important
            }
        }

        public static string ComputeHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));

                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }

                return sb.ToString();
            }
        }

        private static string GetCacheFilePath(string cacheKey)
        {
            var applicationFolder = ComputeHash(AppDomain.CurrentDomain.BaseDirectory.ToLowerInvariant()).Substring(0, 16);
            var directory = Path.Combine(Path.GetTempPath(), CACHE_FOLDER_NAME, applicationFolder);

            return Path.Combine(directory, ComputeHash(cacheKey) + ".dll");
        }

        /// <summary>
        /// Removes assemblies of previous versions. Several containers (and processes) of one application can
        /// share the directory, so the most recently used files are kept instead of deleting everything else.
        /// </summary>
        private static void TryCleanUp(string directory, string currentFileName)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(directory);
                if (!directoryInfo.Exists)
                {
                    return;
                }

                var now = DateTime.UtcNow;

                foreach (var abandonedTempFile in directoryInfo.GetFiles("*.tmp").Where(f => (now - f.LastWriteTimeUtc) > _abandonedTempFileAge))
                {
                    TryDeleteFile(abandonedTempFile.FullName);
                }

                var cachedAssemblies = directoryInfo.GetFiles("*.dll")
                    .Where(f => !string.Equals(f.Name, currentFileName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ToList();

                for (var i = 0; i < cachedAssemblies.Count; i++)
                {
                    var file = cachedAssemblies[i];

                    if ((i >= (MAX_CACHED_ASSEMBLIES_PER_APPLICATION - 1)) || ((now - file.LastWriteTimeUtc) > _maxUnusedCachedAssemblyAge))
                    {
                        TryDeleteFile(file.FullName);
                    }
                }
            }
            catch (Exception)
            {
                // The cleanup is not important
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                // The file can be in use by another process
            }
        }
    }
}
