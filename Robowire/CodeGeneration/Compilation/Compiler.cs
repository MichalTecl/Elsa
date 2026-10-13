using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using CodeGeneration.Primitives;

using Microsoft.CSharp;

namespace CodeGeneration.Compilation
{
    public class Compiler : ICompiler
    {
        private const string CACHE_FORMAT_VERSION = "1";
        private const string CACHE_FOLDER_NAME = "RobowireContainers";
        private const int MAX_CACHED_ASSEMBLIES_PER_APPLICATION = 10;

        private static readonly TimeSpan _maxUnusedCachedAssemblyAge = TimeSpan.FromDays(7);
        private static readonly TimeSpan _abandonedTempFileAge = TimeSpan.FromHours(1);

        private readonly StringBuilder _code = new StringBuilder();
        private readonly HashSet<Assembly> _registeredAssemblies = new HashSet<Assembly>();

        /// <summary>
        /// When enabled, compiled assemblies are stored in %TEMP%\RobowireContainers and reused on the next start
        /// if the generated code and all referenced assemblies are the same.
        /// </summary>
        public static bool UseAssemblyCache { get; set; } = true;

        public ICompiler Write(string value)
        {
            _code.Append(value);
            return this;
        }

        public ICompiler WriteLine(string value = null)
        {
            if (value == null)
            {
                _code.AppendLine();
            }
            else
            {
                _code.AppendLine(value);
            }

            return this;
        }

        public void RegisterType(Type type)
        {
            RegisterAssembly(type.Assembly);
        }

        public void RegisterAssembly(Assembly a)
        {
            if (_registeredAssemblies.Contains(a))
            {
                return;
            }

            _registeredAssemblies.Add(a);

            foreach (var referenced in a.GetReferencedAssemblies())
            {
                if (_registeredAssemblies.Any(i => i.GetName() == referenced))
                {
                    continue;
                }

                var loaded = Assembly.Load(referenced);
                RegisterAssembly(loaded);
            }
        }

        public Assembly Compile()
        {
            var code = _code.ToString();

            var cacheFile = UseAssemblyCache ? TryGetCacheFilePath(code) : null;
            if (cacheFile != null)
            {
                var cached = TryLoadFromCache(cacheFile);
                if (cached != null)
                {
                    return cached;
                }
            }

            var assemblyBytes = CompileToBytes(code);

            if (cacheFile != null)
            {
                TrySaveToCache(cacheFile, assemblyBytes);
            }

            return Assembly.Load(assemblyBytes);
        }

        private byte[] CompileToBytes(string code)
        {
            var outputPath = Path.Combine(Path.GetTempPath(), $"robowire_{Guid.NewGuid():N}.dll");

            try
            {
                using (var provider = new CSharpCodeProvider())
                {
                    var parameters = new CompilerParameters();

                    SetReferencedAssemblies(parameters.ReferencedAssemblies);

                    parameters.GenerateInMemory = false;
                    parameters.GenerateExecutable = false;
                    parameters.OutputAssembly = outputPath;

                    var results = provider.CompileAssemblyFromSource(parameters, code);

                    if (results.Errors.HasErrors)
                    {
                        StringBuilder sb = new StringBuilder();

                        foreach (CompilerError error in results.Errors)
                        {
                            sb.AppendLine(error.ToString());
                        }

                        throw new InvalidOperationException(sb.ToString());
                    }

                    return File.ReadAllBytes(outputPath);
                }
            }
            finally
            {
                TryDeleteFile(outputPath);
            }
        }

        /// <summary>
        /// The cache key covers the generated code and the identity (including the build-specific MVID)
        /// of every referenced assembly, so any rebuild or new deployment results in a different key.
        /// </summary>
        private string TryGetCacheFilePath(string code)
        {
            try
            {
                var applicationFolder = ComputeHash(AppDomain.CurrentDomain.BaseDirectory.ToLowerInvariant()).Substring(0, 16);
                var directory = Path.Combine(Path.GetTempPath(), CACHE_FOLDER_NAME, applicationFolder);

                var assemblyIdentities = _registeredAssemblies
                    .Select(a => $"{a.FullName}|{a.ManifestModule.ModuleVersionId:N}")
                    .OrderBy(i => i, StringComparer.Ordinal);

                var key = new StringBuilder();
                key.AppendLine(CACHE_FORMAT_VERSION);

                foreach (var identity in assemblyIdentities)
                {
                    key.AppendLine(identity);
                }

                key.AppendLine();
                key.Append(code);

                return Path.Combine(directory, ComputeHash(key.ToString()) + ".dll");
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static Assembly TryLoadFromCache(string cacheFile)
        {
            byte[] assemblyBytes;

            try
            {
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

        private static void TrySaveToCache(string cacheFile, byte[] assemblyBytes)
        {
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

            TryCleanUpCache(directory, Path.GetFileName(cacheFile));
        }

        /// <summary>
        /// Removes assemblies of previous versions. Several containers (and processes) of one application can
        /// share the directory, so the most recently used files are kept instead of deleting everything else.
        /// </summary>
        private static void TryCleanUpCache(string directory, string currentFileName)
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

        private static string ComputeHash(string value)
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

        private void SetReferencedAssemblies(StringCollection target)
        {
            foreach (var asm in _registeredAssemblies)
            {
                target.Add(asm.Location);
            }
        }

        public override string ToString()
        {
            return _code.ToString();
        }
    }
}
