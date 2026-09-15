using Elsa.Commerce.Core.Shipment;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Elsa.Integration.ShipmentProviders.Dpd
{
    public class DpdTrackingClient
    {
        private const string API_URL = "https://tracking.dpd.cz/v1/parcels";
        private const int PAGE_SIZE = 100;
        private const int MAX_PAGES = 100;
        private static readonly HttpClient _httpClient = CreateHttpClient();
        private static readonly HashSet<int> _transportNumericStatusCodes = new HashSet<int>
        {
            1, 2, 3, 4, 5, 6, 8, 9, 10, 12, 13, 14, 15, 17, 20, 23
        };
        private static readonly HashSet<string> _transportTextStatusCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PKY", "ORI", "ORO", "HUI", "HUO", "DLI", "DLO", "DEY", "DEN", "DEX",
            "DEHD", "DODEH", "DODEI", "DODEY", "DOPKY", "DEYY"
        };

        private readonly DpdClientConfig _config;

        public DpdTrackingClient(DpdClientConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public IReadOnlyCollection<string> FindParcelNumbers(string parcelReference)
        {
            if (string.IsNullOrWhiteSpace(parcelReference))
                throw new ArgumentException("Reference zásilky nesmí být prázdná.", nameof(parcelReference));
            EnsureConfigured();

            var normalizedReference = parcelReference.Trim();
            var allNumbers = new HashSet<string>(StringComparer.Ordinal);
            for (var page = 0; page < MAX_PAGES; page++)
            {
                var pageNumbers = ReadPage(normalizedReference, page * PAGE_SIZE);
                foreach (var number in pageNumbers)
                    allNumbers.Add(number);

                if (pageNumbers.Count < PAGE_SIZE)
                    return allNumbers.OrderBy(number => number, StringComparer.Ordinal).ToArray();
            }

            throw new InvalidOperationException($"DPD Tracking API překročilo limit {MAX_PAGES * PAGE_SIZE} zásilek pro jednu referenci.");
        }

        public ShipmentTrackingInfo GetTrackingInfo(string parcelNumber)
        {
            if (string.IsNullOrWhiteSpace(parcelNumber))
                throw new ArgumentException("Číslo zásilky nesmí být prázdné.", nameof(parcelNumber));

            EnsureConfigured();
            var normalizedNumber = parcelNumber.Trim();
            var responseBody = SendGet($"{API_URL}/{Uri.EscapeDataString(normalizedNumber)}");
            return ParseTrackingInfo(responseBody, normalizedNumber, DateTime.Now);
        }

        private IReadOnlyCollection<string> ReadPage(string parcelReference, int offset)
        {
            var requestUrl = $"{API_URL}?parcel-ref={Uri.EscapeDataString(parcelReference)}&offset={offset}&limit={PAGE_SIZE}";
            return ParseParcelNumbers(SendGet(requestUrl));
        }

        private string SendGet(string requestUrl)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, requestUrl))
            {
                request.Headers.Add("x-api-key", _config.TrackingApiKey.Trim());

                using (var response = _httpClient.SendAsync(request).GetAwaiter().GetResult())
                {
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                            throw new InvalidOperationException(CreateAuthorizationErrorMessage(
                                response.StatusCode,
                                request.RequestUri,
                                responseBody));

                        throw new InvalidOperationException($"DPD Tracking API vrátilo HTTP {(int)response.StatusCode} {response.ReasonPhrase}.");
                    }

                    return responseBody;
                }
            }
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_config.TrackingApiKey))
                throw new InvalidOperationException("Chybí konfigurace Dpd.TrackingApiKey.");
        }

        private string GetApiKeyDiagnostic()
        {
            var apiKey = _config.TrackingApiKey.Trim();
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
                var fingerprint = string.Concat(hash.Take(6).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
                return $"délka {apiKey.Length}, SHA-256 {fingerprint}";
            }
        }

        private string CreateAuthorizationErrorMessage(HttpStatusCode statusCode, Uri requestUri, string responseBody)
        {
            try
            {
                var error = JObject.Parse(responseBody);
                var code = (string)error["code"];
                var message = (string)error["message"];
                var unauthorizedParcels = error["details"]?["unauthorizedParcels"]?.Values<string>()
                    .Where(parcelNumber => !string.IsNullOrWhiteSpace(parcelNumber))
                    .Select(parcelNumber => parcelNumber.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                if (string.Equals(code, "UNAUTHORIZED", StringComparison.OrdinalIgnoreCase)
                    && unauthorizedParcels != null
                    && unauthorizedParcels.Length > 0)
                {
                    return $"DPD Tracking API našlo zásilky mimo zákaznické účty přiřazené použitému klíči: " +
                           $"{string.Join(", ", unauthorizedParcels)}. Může jít o shodu zákaznické reference " +
                           $"s cizí zásilkou nebo o chybějící přiřazení zákaznického DSW k API uživateli. " +
                           $"Endpoint: {requestUri.PathAndQuery}. Použitý klíč: {GetApiKeyDiagnostic()}.";
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    return $"DPD Tracking API odmítlo požadavek (HTTP {(int)statusCode}): {message.Trim()} " +
                           $"Endpoint: {requestUri.PathAndQuery}. Použitý klíč: {GetApiKeyDiagnostic()}.";
                }
            }
            catch (JsonException)
            {
            }

            return $"DPD Tracking API odmítlo požadavek (HTTP {(int)statusCode}). " +
                   $"Endpoint: {requestUri.PathAndQuery}. Použitý klíč: {GetApiKeyDiagnostic()}.";
        }

        internal static IReadOnlyCollection<string> ParseParcelNumbers(string responseBody)
        {
            JToken response;
            try
            {
                response = JToken.Parse(responseBody);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("DPD Tracking API nevrátilo očekávaný JSON.", exception);
            }

            if (response.Type != JTokenType.Array)
                throw new InvalidOperationException("DPD Tracking API vrátilo neočekávaný formát odpovědi.");

            var numbers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var resultToken in response.Children())
            {
                var result = resultToken as JObject;
                if (result == null)
                    throw new InvalidOperationException("DPD Tracking API vrátilo neočekávanou položku odpovědi.");

                var data = result["data"];
                if (data == null || data.Type == JTokenType.Null)
                    continue;

                IEnumerable<JToken> parcels;
                if (data.Type == JTokenType.Array)
                    parcels = data.Children();
                else
                    parcels = new[] { data };

                foreach (var parcel in parcels)
                {
                    var parcelData = parcel as JObject;
                    var number = parcelData?["parcelInfo"]?["parcelNumber"];
                    if (number == null || number.Type != JTokenType.String || string.IsNullOrWhiteSpace((string)number))
                        throw new InvalidOperationException("DPD Tracking API vrátilo zásilku bez očekávaného parcelInfo.parcelNumber.");

                    numbers.Add(((string)number).Trim());
                }
            }

            return numbers.OrderBy(number => number, StringComparer.Ordinal).ToArray();
        }

        internal static ShipmentTrackingInfo ParseTrackingInfo(string responseBody, string expectedParcelNumber, DateTime observedAt)
        {
            JObject response;
            try
            {
                response = JObject.Parse(responseBody);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("DPD Tracking API nevrátilo očekávaný JSON detailu zásilky.", exception);
            }

            var returnedParcelNumber = (string)response["parcelInfo"]?["parcelNumber"];
            if (string.IsNullOrWhiteSpace(returnedParcelNumber)
                || !string.Equals(returnedParcelNumber.Trim(), expectedParcelNumber, StringComparison.Ordinal))
                throw new InvalidOperationException("DPD Tracking API vrátilo detail jiné zásilky nebo zásilku bez parcelInfo.parcelNumber.");

            var eventsToken = response["trackingEvents"];
            if (eventsToken == null || eventsToken.Type != JTokenType.Array)
                throw new InvalidOperationException("DPD Tracking API vrátilo zásilku bez očekávaného seznamu trackingEvents.");

            var events = eventsToken.Children().Select(ParseTrackingEvent).OrderBy(item => item.EventTime).ToList();
            return new ShipmentTrackingInfo
            {
                ParcelRegistered = events.Count == 0 ? observedAt : events[0].EventTime,
                ParcelTransportStarted = events.Where(IsTransportEvent)
                    .Select(item => (DateTime?)item.EventTime)
                    .FirstOrDefault(),
                Delivered = events.Where(IsDeliveredEvent)
                    .Select(item => (DateTime?)item.EventTime)
                    .FirstOrDefault()
            };
        }

        private static DpdTrackingEvent ParseTrackingEvent(JToken eventToken)
        {
            var eventObject = eventToken as JObject;
            var status = eventObject?["status"] as JObject;
            var code = (string)status?["code"];
            var englishDescription = (string)status?["description"]?["en"];
            var eventTimeText = (string)eventObject?["eventTime"];

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(englishDescription))
                throw new InvalidOperationException("DPD Tracking API vrátilo událost bez kódu nebo anglického popisu stavu.");
            if (!DateTimeOffset.TryParse(eventTimeText, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var eventTime))
                throw new InvalidOperationException("DPD Tracking API vrátilo událost bez platného eventTime.");

            return new DpdTrackingEvent(code.Trim(), englishDescription.Trim(), eventTime.LocalDateTime);
        }

        private static bool IsTransportEvent(DpdTrackingEvent trackingEvent)
        {
            if (_transportTextStatusCodes.Contains(trackingEvent.StatusCode))
                return true;

            return int.TryParse(trackingEvent.StatusCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericCode)
                   && _transportNumericStatusCodes.Contains(numericCode);
        }

        private static bool IsDeliveredEvent(DpdTrackingEvent trackingEvent)
        {
            if (string.Equals(trackingEvent.StatusCode, "DEY", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trackingEvent.StatusCode, "DODEY", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.Equals(trackingEvent.StatusCode, "13", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(trackingEvent.StatusCode, "DEYY", StringComparison.OrdinalIgnoreCase))
                return false;

            var description = trackingEvent.EnglishDescription.ToLowerInvariant();
            return (description.Contains("consignee") || description.Contains("recipient") || description.Contains("customer"))
                   && !description.Contains("sender")
                   && !description.Contains("return");
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private sealed class DpdTrackingEvent
        {
            public DpdTrackingEvent(string statusCode, string englishDescription, DateTime eventTime)
            {
                StatusCode = statusCode;
                EnglishDescription = englishDescription;
                EventTime = eventTime;
            }

            public string StatusCode { get; }

            public string EnglishDescription { get; }

            public DateTime EventTime { get; }
        }
    }
}
