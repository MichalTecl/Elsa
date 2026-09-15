using Elsa.Commerce.Core.Shipment;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    public class PacketaTrackingClient
    {
        private const string API_URL = "https://www.zasilkovna.cz/api/rest";
        private const int REGISTERED_STATUS = 1;
        private const int DELIVERED_STATUS = 7;
        private const int CANCELLED_STATUS = 11;
        private const int COURIER_TRACKING_CODE_ADDED_STATUS = 31;
        private const int UNKNOWN_STATUS = 999;
        private static readonly HttpClient _httpClient = CreateHttpClient();

        private readonly ZasilkovnaClientConfig _config;

        public PacketaTrackingClient(ZasilkovnaClientConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public ShipmentTrackingInfo GetTrackingInfo(string packetNumber)
        {
            if (string.IsNullOrWhiteSpace(packetNumber))
                throw new ArgumentException("Číslo zásilky nesmí být prázdné.", nameof(packetNumber));
            if (string.IsNullOrWhiteSpace(_config.ApiToken))
                throw new InvalidOperationException("Chybí konfigurace Zasilkovna.ApiToken.");

            var requestDocument = new XDocument(
                new XElement("packetTracking",
                    new XElement("apiPassword", _config.ApiToken.Trim()),
                    new XElement("packetId", packetNumber.Trim())));

            using (var content = new StringContent(requestDocument.ToString(SaveOptions.DisableFormatting), Encoding.UTF8, "application/xml"))
            using (var response = _httpClient.PostAsync(API_URL, content).GetAwaiter().GetResult())
            {
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Packeta Tracking API vrátilo HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");

                return ParseTrackingInfo(responseBody, DateTime.Now);
            }
        }

        internal static ShipmentTrackingInfo ParseTrackingInfo(string responseBody, DateTime observedAt)
        {
            XDocument document;
            try
            {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit };
                using (var stringReader = new StringReader(responseBody))
                using (var xmlReader = XmlReader.Create(stringReader, settings))
                    document = XDocument.Load(xmlReader);
            }
            catch (XmlException exception)
            {
                throw new InvalidOperationException("Packeta Tracking API nevrátilo očekávané XML.", exception);
            }

            var root = document.Root;
            if (root == null || root.Name.LocalName != "response")
                throw new InvalidOperationException("Packeta Tracking API vrátilo neočekávaný formát odpovědi.");

            var status = GetSingleChildValue(root, "status");
            if (!string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Packeta Tracking API vrátilo stav '{status ?? "bez stavu"}'.");

            var result = GetSingleChild(root, "result");
            var records = result.Elements().Where(element => element.Name.LocalName == "record")
                .Select(ParseRecord)
                .OrderBy(record => record.DateTime)
                .ToList();

            return new ShipmentTrackingInfo
            {
                ParcelRegistered = records.Count == 0 ? observedAt : records[0].DateTime,
                ParcelTransportStarted = records.Where(record => IsTransportStatus(record.StatusCode))
                    .Select(record => (DateTime?)record.DateTime)
                    .FirstOrDefault(),
                Delivered = records.Where(record => record.StatusCode == DELIVERED_STATUS)
                    .Select(record => (DateTime?)record.DateTime)
                    .FirstOrDefault()
            };
        }

        private static TrackingRecord ParseRecord(XElement element)
        {
            var statusCodeText = GetSingleChildValue(element, "statusCode");
            if (!int.TryParse(statusCodeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var statusCode))
                throw new InvalidOperationException("Packeta Tracking API vrátilo událost bez platného statusCode.");

            var dateTimeText = GetSingleChildValue(element, "dateTime");
            if (!DateTime.TryParse(dateTimeText, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTime))
                throw new InvalidOperationException("Packeta Tracking API vrátilo událost bez platného dateTime.");

            return new TrackingRecord(statusCode, dateTime);
        }

        private static bool IsTransportStatus(int statusCode)
        {
            return statusCode != REGISTERED_STATUS
                   && statusCode != CANCELLED_STATUS
                   && statusCode != COURIER_TRACKING_CODE_ADDED_STATUS
                   && statusCode != UNKNOWN_STATUS;
        }

        private static XElement GetSingleChild(XElement parent, string name)
        {
            var elements = parent.Elements().Where(element => element.Name.LocalName == name).ToList();
            if (elements.Count != 1)
                throw new InvalidOperationException($"Packeta Tracking API vrátilo neočekávaný počet elementů '{name}'.");
            return elements[0];
        }

        private static string GetSingleChildValue(XElement parent, string name)
        {
            return GetSingleChild(parent, name).Value?.Trim();
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            return client;
        }

        private sealed class TrackingRecord
        {
            public TrackingRecord(int statusCode, DateTime dateTime)
            {
                StatusCode = statusCode;
                DateTime = dateTime;
            }

            public int StatusCode { get; }

            public DateTime DateTime { get; }
        }
    }
}
