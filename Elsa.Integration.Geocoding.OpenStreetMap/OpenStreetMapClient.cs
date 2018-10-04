using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Integration.Geocoding.Common;
using Elsa.Integration.Geocoding.OpenStreetMap.Model;

using Newtonsoft.Json;

namespace Elsa.Integration.Geocoding.OpenStreetMap
{
    public class OpenStreetMapClient : IGeocodingProvider
    {
        private static readonly HttpClient s_httpClient = new HttpClient();

        private static DateTime s_lastCall = DateTime.MinValue;

        static OpenStreetMapClient()
        {
            s_httpClient.DefaultRequestHeaders.Add("User-Agent", "MTecl_karlin");
        }
        
        public void SetLatLon(IAddress address, bool skipStreetNumber)
        {
            if ((DateTime.Now - s_lastCall).TotalSeconds < 1)
            {
                Thread.Sleep(1000);
            }

            var query = address.GeoQuery;

            if (skipStreetNumber)
            {
                query = null;
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                var hNumber = string.IsNullOrWhiteSpace(address.DescriptiveNumber)
                                  ? address.OrientationNumber
                                  : address.DescriptiveNumber;

                if (skipStreetNumber)
                {
                    hNumber = string.Empty;
                }

                var hStreet = StringUtil.RemoveNumericChars(address.Street).Trim();
                hStreet = string.Join(" ", hStreet.Split('.', ',').Where(s => (s.Trim()).Length > 1).Select(s => s.Trim()));

                hStreet = hStreet.Split('(').First().Trim();

                int dummy;
                if (string.IsNullOrWhiteSpace(hStreet) || !int.TryParse(hNumber, out dummy))
                {
                    hNumber = string.Empty;
                }

                var hCity = StringUtil.RemoveNumericChars(address.City ?? string.Empty).Trim();
                
                var hStrNum = string.IsNullOrWhiteSpace(hStreet) ? string.Empty :  $"{hStreet.Trim()} {hNumber.Trim()}".Trim();
                if (!string.IsNullOrWhiteSpace(hStrNum))
                {
                    hStrNum = $"{hStrNum}, ";
                }

                query = $"{hStrNum}{address.Zip} {hCity}";
                address.GeoQuery = StringUtil.Limit(query, 512);
            }

            var addrString = Uri.EscapeDataString(query);

            var url = $"https://nominatim.openstreetmap.org/search?q={addrString}&format=json";

            s_lastCall = DateTime.Now;
            var response = s_httpClient.GetStringAsync(url).Result;

                var model = JsonConvert.DeserializeObject<List<OsmGeoResponse>>(response)?.FirstOrDefault();
            if (model == null)
            {
                if (!skipStreetNumber)
                {
                    SetLatLon(address, true);
                    return;
                }

                address.GeoInfo = "UNKNOWN";
            }
            else
            {
                address.Lat = StringUtil.Limit(model.Lat, 16);
                address.Lon = StringUtil.Limit(model.Lon, 16);

                if (model.Display_Name?.Length > 512)
                {
                    model.Display_Name = model.Display_Name.Substring(0, 500) + "...";
                }
                address.GeoInfo =  model.Display_Name;
            }
        }

        public void SetLatLon(IAddress address)
        {
            SetLatLon(address, false);
        }
    }
}
