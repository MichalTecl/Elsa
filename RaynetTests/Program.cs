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

            var contact = rn.GetContactDetail(43);

            var eshop = rn.ReadCustomField<bool>(contact.Data, "E-shop");
            var store = rn.ReadCustomField<bool>(contact.Data, "Kamenná prodejna");
        }
    }
}
