using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Elsa.Common
{
    public static class ReleaseVersionInfo
    {
        private static readonly Lazy<string> s_releaseTag = new Lazy<string>(LoadReleaseTag, LazyThreadSafetyMode.ExecutionAndPublication);

        public static string Tag => s_releaseTag.Value;

        private static string LoadReleaseTag()
        {
            try
            {
                var context = HttpContext.Current;
                if (context == null)
                {
                    throw new InvalidOperationException("NotInHttpContext");
                }

                var home = Path.GetDirectoryName(context.Server.MapPath("~/Home.html"));

                byte[] hashBytes;
                using (var md5 = new MD5CryptoServiceProvider())
                {
                    md5.Initialize();
                    hashBytes = md5.ComputeHash(FillBuffer(ReadFileStruct(home), 4096));
                }

                return HttpUtility.UrlEncode(hashBytes);
            }
            catch (Exception ex)
            {
                return $"{DateTime.Now.Ticks}{ex.Message.Replace(' ', '_')}";
            }
        }

        private static byte[] FillBuffer(IEnumerable<byte> src, int size)
        {
            var buffer = new byte[size];
            var i = 0;
            foreach (var b in src)
            {
                buffer[i] = (byte)(b ^ buffer[i]);

                i = (i + 1) % size;
            }

            return buffer;
        }

        private static IEnumerable<byte> ReadFileStruct(string home)
        {
            foreach (var f in Directory.GetFiles(home, "*.*", SearchOption.AllDirectories))
            {
                var fileInfo = new FileInfo(f);

                foreach (var nchar in Encoding.ASCII.GetBytes(fileInfo.FullName))
                {
                    yield return nchar;
                }

                foreach (var h in BitConverter.GetBytes(fileInfo.CreationTimeUtc.Ticks))
                {
                    yield return h;
                }

                foreach (var h in BitConverter.GetBytes(fileInfo.Length))
                {
                    yield return h;
                }

                foreach (var h in BitConverter.GetBytes(fileInfo.LastWriteTime.Ticks))
                {
                    yield return h;
                }
            }
        }
    }
}
