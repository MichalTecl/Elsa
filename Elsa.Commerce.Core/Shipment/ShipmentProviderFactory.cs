using System;
using System.Linq;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Integration;
using Robowire;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Shipment
{
    public class ShipmentProviderFactory : IShipmentProviderFactory
    {
        private readonly IServiceLocator _services;
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly ISession _session;

        public ShipmentProviderFactory(IServiceLocator services, IDatabase database, ICache cache, ISession session)
        {
            _services = services;
            _database = database;
            _cache = cache;
            _session = session;
        }

        public IShipmentProvider GetBySymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Název dopravce nesmí být prázdný.", nameof(symbol));

            var providers = _services.GetCollection<IShipmentProvider>().ToList();
            var matching = providers.Where(p => string.Equals(p.Symbol, symbol, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (matching.Count == 0)
                throw new InvalidOperationException($"Dopravce '{symbol}' není registrován. Dostupní dopravci: [{string.Join(", ", providers.Select(p => p.Symbol))}]");

            if (matching.Count > 1)
                throw new InvalidOperationException($"Pro dopravce '{symbol}' je registrováno více implementací.");

            return matching[0];
        }

        public IShipmentProvider GetByShipmentMethod(string shipmentMethodText)
        {
            if (string.IsNullOrWhiteSpace(shipmentMethodText))
                throw new ArgumentException("Způsob dopravy nesmí být prázdný.", nameof(shipmentMethodText));

            var lookup = _cache.ReadThrough($"shipmentProviderLookup_{_session.Project.Id}", TimeSpan.FromSeconds(10), () =>
            {
                return _database.SelectFrom<IShipmentProviderLookup>()
                    .Where(p => p.ProjectId == _session.Project.Id).Execute().ToList();
            });

            var names = lookup.Where(p => StringUtil.MatchStarWildcard(p.ShipMethodWildcardPattern, shipmentMethodText))
                .Select(p => p.ProviderName)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            if (names.Count != 1)
                throw new InvalidOperationException($"Pro způsob dopravy '{shipmentMethodText}' nelze jednoznačně určit dopravce. Nalezení dopravci: [{string.Join(", ", names)}]");

            return GetBySymbol(names[0]);
        }
    }
}
