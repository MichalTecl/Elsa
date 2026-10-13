using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using CodeGeneration.Compilation;

namespace Robowire
{
    /// <summary>
    /// Identifies the build of the running application: a hash of name, size and modification time of all
    /// assemblies in the application's bin folder(s). File contents are not read, so it is cheap to compute.
    /// </summary>
    public static class ApplicationBuild
    {
        private static readonly Lazy<string> _hash = new Lazy<string>(ComputeHash, LazyThreadSafetyMode.ExecutionAndPublication);

        public static string Hash => _hash.Value;

        private static string ComputeHash()
        {
            var sb = new StringBuilder();

            sb.AppendLine(Environment.Version.ToString());
            sb.AppendLine(typeof(object).Assembly.ManifestModule.ModuleVersionId.ToString("N"));

            foreach (var directory in GetBinDirectories())
            {
                sb.AppendLine(directory.ToLowerInvariant());

                var files = new DirectoryInfo(directory).GetFiles()
                    .Where(f => f.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) || f.Extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    sb.Append(file.Name.ToLowerInvariant()).Append('|').Append(file.Length).Append('|').AppendLine(file.LastWriteTimeUtc.Ticks.ToString());
                }
            }

            return AssemblyCache.ComputeHash(sb.ToString());
        }

        private static string[] GetBinDirectories()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var searchPath = AppDomain.CurrentDomain.RelativeSearchPath;

            if (string.IsNullOrWhiteSpace(searchPath))
            {
                return new[] { Path.GetFullPath(baseDirectory) };
            }

            return searchPath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => Path.GetFullPath(Path.Combine(baseDirectory, p.Trim())))
                .Where(Directory.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
