using Elsa.Integration.Crm.Raynet;
using Elsa.Test.Utils;
using System;

namespace RaynetTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var protocol = new RnProtocol(ConfigFactory.Get<RaynetClientConfig>(), ConsoleLogger.Instance);
            IRaynetClient rn = new RnActions(protocol);

            var contacts = rn.GetBusinessCases("2307298");
        }
    }
}
