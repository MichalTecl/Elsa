using Elsa.Common.Logging;
using Robowire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Shipment
{
    public class ShipmentRequestGeneratorFactory
    {
        private readonly IServiceLocator _services;

        public ShipmentRequestGeneratorFactory(IServiceLocator services)
        {
            _services = services;
        }

        public IShipmentRequestDocumentGenerator Get(string symbol)
        {           
            var coll = _services.GetCollection<IShipmentRequestDocumentGenerator>();

            var generator = coll.FirstOrDefault(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

            if (generator == null)
                throw new Exception($"Unknow generator symbol '{symbol}'. Existing symbols are: {string.Join(", ", coll.Select(c => c.Symbol))}");

            return generator;
        }
    }
}
