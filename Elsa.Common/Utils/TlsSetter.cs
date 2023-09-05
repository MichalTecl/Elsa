using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Elsa.Common.Utils
{
    public static class TlsSetter
    {
        public static void Setup()
        {
            var changed = false;
            SecurityProtocolType spt = default;
            foreach (var protype in LoadSecurityProtcolsFile())
            {
                spt |= protype;
                changed = true;
            }

            if (!changed)
                return;

            System.Net.ServicePointManager.SecurityProtocol = spt;
        }

        private static IEnumerable<SecurityProtocolType> LoadSecurityProtcolsFile()
        {
            Directory.CreateDirectory("C:\\Elsa\\Settings");

            var file = "C:\\Elsa\\Settings\\SecurityProtocolTypes";

            if (!File.Exists(file))
                File.WriteAllLines(file, new[] { 
                    "# Delete this file to let it regenerate with all existing security protocol types.", 
                    "# Or keep only commented lines (like this one) to not affect System.Net.ServicePointManager.SecurityProtocol" }
                .Concat(Enum.GetNames(typeof(SecurityProtocolType))));

            foreach (var sType in File.ReadAllLines(file))
            {
                if (string.IsNullOrWhiteSpace(sType) || (!Enum.TryParse<SecurityProtocolType>(sType, out var secType)))
                    continue;

                yield return secType;
            }
        }
    }
}
