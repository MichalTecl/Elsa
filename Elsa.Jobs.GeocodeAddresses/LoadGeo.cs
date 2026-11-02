using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Integration.Geocoding.Common;
using Elsa.Jobs.Common;

using RobOrm.Core;

namespace Elsa.Jobs.GeocodeAddresses
{
    public class LoadGeo : IExecutableJob
    {
        private readonly IDatabase _database;

        private readonly ISession _session;

        private readonly IGeocodingProvider _geocodingProvider;

        private readonly ILog _log;


        public LoadGeo(IDatabase database, ISession session, IGeocodingProvider geocodingProvider, ILog log)
        {
            _database = database;
            _session = session;
            _geocodingProvider = geocodingProvider;
            _log = log;
        }

        public void Run(string customDataJson)
        {
            _database.Sql().Execute(@"UPDATE Address SET Zip = REPLACE(Zip, ' ', '') WHERE Zip like '% %'").NonQuery();

            var zips = new List<string>();
            
            _database.Sql().Execute("SELECT DISTINCT Zip FROM Address WHERE GeoInfo IS NULL").ReadRows<string>(r => zips.Add(r));
            
            foreach (var zip in zips)
            {
                _log.Info($"Processing ZIP {zip}");
                var allAddresses = _database.SelectFrom<IAddress>().Where(a => a.Zip == zip).Execute().ToList();
                var sources = allAddresses.Where(a => a.GeoInfo != null).ToList();

                var addresses = allAddresses.Where(a => a.GeoInfo == null).ToList();
                if (!addresses.Any())
                {
                    _log.Info("All addresses resolved");
                    continue;
                }

                _log.Info($"Loaded {addresses.Count} addresses to resolve");

                int fromApi = 0;
                int reused = 0;

                foreach (var address in addresses)
                {
                    var src =
                        sources.LastOrDefault(
                            s =>
                                s.City == address.City && s.Street == address.Street
                                && (address.DescriptiveNumber == s.DescriptiveNumber
                                    || address.OrientationNumber == s.OrientationNumber))
                      ?? sources.LastOrDefault(
                            s =>
                                s.City == address.City && s.Street == address.Street);

                    if (src != null)
                    {
                        reused++;
                        address.Lat = src.Lat;
                        address.Lon = src.Lon;
                        address.GeoInfo = src.GeoInfo;
                        address.GeoQuery = src.GeoQuery;
                    }
                    else
                    {
                        fromApi++;
                        _geocodingProvider.SetLatLon(address);
                        sources.Add(address);
                    }
                    
                    _database.Save(address);
                }

                _log.Info($"API: {fromApi}, Reused: {reused}");

            }

        }
    }
}