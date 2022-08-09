using Elsa.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public static class DirectorySizeKeeper
    {
        public static void KeepSize(string directoryPath, int maxSize, int newSize, ILog log)
        {
            if(!Directory.Exists(directoryPath))
            {
                log.Info($"Directory '{directoryPath}' does not exist");
            }

            var files = GetFiles(directoryPath);

            var totalSize = files.Sum(f => f.Length);
                        
            if (totalSize < maxSize)
            {
                log.Info($"Directory {directoryPath} has total size {totalSize}B lower than threshold {maxSize}B");
                return;
            }

            log.Info($"Directory {directoryPath} has total size {totalSize}B higher than threshold {maxSize}B");

            var fromOldest = files.OrderBy(f => f.CreationTimeUtc);

            foreach(var f in fromOldest)
            {
                totalSize -= f.Length;
                File.Delete(f.FullName);
                log.Info($"File {f.FullName} was deleted");

                if (totalSize <= newSize)
                {
                    return;
                }
            }
            
        }

        private static List<FileInfo> GetFiles(string directory)
        {
            return Directory.GetFiles(directory).Select(i => new FileInfo(i)).ToList();
        }
    }
}
