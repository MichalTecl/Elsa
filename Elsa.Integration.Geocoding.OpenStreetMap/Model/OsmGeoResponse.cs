namespace Elsa.Integration.Geocoding.OpenStreetMap.Model
{
    public class OsmGeoResponse
    {
        public string Lat { get; set; }

        public string Lon { get; set; }

        // ReSharper disable once InconsistentNaming
        public string Display_Name { get; set; }
    }
}
