using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.Common.EntityChangeProcessing.Helpers
{
    internal static class EntityHashCalculator
    {
        public static string GetHashCode(IEnumerable<object> source) 
        {
            var srbytes = source.Select(s => s?.ToString() ?? string.Empty).Select(s => Encoding.UTF8.GetBytes(s)).SelectMany(s => s).ToArray();

            using (var md5 = new MD5CryptoServiceProvider()) 
            {
                var hash = md5.ComputeHash(srbytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
