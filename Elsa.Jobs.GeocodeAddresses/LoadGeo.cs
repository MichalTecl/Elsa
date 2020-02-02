using System.Collections.Generic;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Integration.Geocoding.Common;
using Elsa.Jobs.Common;

using Robowire.RobOrm.Core;

namespace Elsa.Jobs.GeocodeAddresses
{
    public class LoadGeo : IExecutableJob
    {
        private readonly IDatabase m_database;

        private readonly ISession m_session;

        private readonly IGeocodingProvider m_geocodingProvider;

        private readonly ILog m_log;


        public LoadGeo(IDatabase database, ISession session, IGeocodingProvider geocodingProvider, ILog log)
        {
            m_database = database;
            m_session = session;
            m_geocodingProvider = geocodingProvider;
            m_log = log;
        }

        public void Run(string customDataJson)
        {
            m_database.Sql().Execute(@"UPDATE Address SET Zip = REPLACE(Zip, ' ', '') WHERE Zip like '% %'").NonQuery();

            var zips = new List<string>();
            
            m_database.Sql().Execute("SELECT DISTINCT Zip FROM Address WHERE GeoInfo IS NULL").ReadRows<string>(r => zips.Add(r));
            
            foreach (var zip in zips)
            {
                m_log.Info($"Processing ZIP {zip}");
                var allAddresses = m_database.SelectFrom<IAddress>().Where(a => a.Zip == zip).Execute().ToList();
                var sources = allAddresses.Where(a => a.GeoInfo != null).ToList();

                var addresses = allAddresses.Where(a => a.GeoInfo == null).ToList();
                if (!addresses.Any())
                {
                    m_log.Info("All addresses resolved");
                    continue;
                }

                m_log.Info($"Loaded {addresses.Count} addresses to resolve");

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
                        m_geocodingProvider.SetLatLon(address);
                        sources.Add(address);
                    }
                    
                    m_database.Save(address);
                }

                m_log.Info($"API: {fromApi}, Reused: {reused}");

            }

        }
    }
}