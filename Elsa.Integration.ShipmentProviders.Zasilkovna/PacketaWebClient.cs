using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    internal sealed class PacketaWebClient : IDisposable
    {
        private const int MAX_PAGES = 1000;
        private static readonly Uri _origin = new Uri("https://client.packeta.com/");
        private readonly HttpClient _http;

        private PacketaWebClient()
        {
            _http = new HttpClient(new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            }) { Timeout = TimeSpan.FromSeconds(60) };
        }

        public static Dictionary<string, HashSet<string>> LoadShipments(ZasilkovnaClientConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.WebLoginUserName) || string.IsNullOrWhiteSpace(config.WebLoginPassword))
                throw new InvalidOperationException("Chybí konfigurace Zasilkovna.WebLoginUserName nebo Zasilkovna.WebLoginPassword.");
            if (string.IsNullOrWhiteSpace(config.ClientName))
                throw new InvalidOperationException("Chybí konfigurace Zasilkovna.ClientName pro výběr odesílatele zásilek.");

            // A fresh cookie jar also prevents inheriting filters from the user's browser.
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using (var client = new PacketaWebClient())
                {
                    client.Login(config);
                    try
                    {
                        return client.ReadShipments(config.ClientName.Trim());
                    }
                    catch (SessionExpiredException)
                    {
                        if (attempt == 1)
                            throw new InvalidOperationException("Přihlášení do klientské sekce Zásilkovny vypršelo i po opakovaném přihlášení.");
                    }
                }
            }
            throw new InvalidOperationException("Nepodařilo se načíst zásilky ze Zásilkovny.");
        }

        private void Login(ZasilkovnaClientConfig config)
        {
            var loginUri = new Uri(_origin, "cs/sign/in");
            var document = ParseHtml(Send(loginUri));
            var form = document.GetElementbyId("frm-signInForm");
            if (form == null)
                throw new InvalidOperationException("Zásilkovna nevrátila očekávaný přihlašovací formulář.");

            var fields = new Dictionary<string, string>();
            foreach (var input in form.Descendants("input"))
            {
                var type = input.GetAttributeValue("type", "");
                var name = input.GetAttributeValue("name", "");
                if (!string.IsNullOrEmpty(name) && (type == "hidden" || type == "submit"))
                    fields[name] = HtmlEntity.DeEntitize(input.GetAttributeValue("value", ""));
            }
            if (!fields.ContainsKey("_do") || !form.Descendants("input").Any(n => n.GetAttributeValue("name", "") == "email")
                || !form.Descendants("input").Any(n => n.GetAttributeValue("name", "") == "password"))
                throw new InvalidOperationException("Změnil se přihlašovací formulář Zásilkovny.");

            fields["email"] = config.WebLoginUserName;
            fields["password"] = config.WebLoginPassword;
            var target = new Uri(loginUri, HtmlEntity.DeEntitize(form.GetAttributeValue("action", "")));
            var response = Send(target, fields);
            if (ParseHtml(response).GetElementbyId("frm-signInForm") != null)
                throw new InvalidOperationException("Přihlášení do Zásilkovny se nezdařilo. Ověřte přihlašovací údaje a případný požadavek na další ověření.");
        }

        private Dictionary<string, HashSet<string>> ReadShipments(string sender)
        {
            // Reset saved filters before requesting the complete list.
            var resetGrid = ReadGrid(new Uri(_origin, "cs/packets/list?do=list-resetFilter"));
            EnsureOnlyDefaultFilters(resetGrid);

            var next = new Uri(_origin, "cs/packets/list?list-page=1&list-perPage=500&list-sort%5Bid%5D=DESC&do=list-page");
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            for (var page = 0; next != null; page++)
            {
                if (page >= MAX_PAGES || !visited.Add(next.AbsoluteUri))
                    throw new InvalidOperationException("Seznam zásilek Zásilkovny nelze dokončit: překročen limit nebo opakovaná stránka.");

                var grid = ReadGrid(next);
                EnsureOnlyDefaultFilters(grid);
                var snippets = grid["snippets"] as JObject;
                var rows = snippets?["snippet-list-tbody"];
                var pagination = snippets?["snippet-list-pagination"];
                if (rows?.Type != JTokenType.String || pagination?.Type != JTokenType.String)
                    throw new InvalidOperationException("V odpovědi Zásilkovny chybí tabulka zásilek nebo stránkování.");

                PacketaShipmentListParser.AddRows((string)rows, sender, result);
                var nextLink = ParseHtml((string)pagination).DocumentNode.Descendants("a")
                    .SingleOrDefault(n => n.GetAttributeValue("rel", "") == "next");
                next = nextLink == null ? null : new Uri(_origin, HtmlEntity.DeEntitize(nextLink.GetAttributeValue("href", "")));
                if (next != null && next.AbsolutePath != "/cs/packets/list")
                    throw new InvalidOperationException("Zásilkovna vrátila neočekávaný odkaz na další stránku.");
            }
            return result;
        }

        private static void EnsureOnlyDefaultFilters(JObject grid)
        {
            var filters = grid["non_empty_filters"] as JArray;
            if (filters == null)
                throw new InvalidOperationException("V seznamu zásilek Zásilkovny chybí informace o aktivních filtrech.");

            var unexpectedFilters = filters.Values<string>()
                .Where(filter => !string.Equals(filter, "dateStored", StringComparison.Ordinal))
                .ToList();

            if (unexpectedFilters.Count > 0)
                throw new InvalidOperationException($"Seznam zásilek Zásilkovny obsahuje neočekávané filtry: {string.Join(", ", unexpectedFilters)}.");
        }

        private JObject ReadGrid(Uri uri)
        {
            var response = Send(uri, ajax: true);
            if (ParseHtml(response).GetElementbyId("frm-signInForm") != null)
                throw new SessionExpiredException();
            JObject grid;
            try { grid = JObject.Parse(response); }
            catch (JsonException) { throw new InvalidOperationException("Zásilkovna nevrátila očekávaný JSON seznamu zásilek. Přihlášení nebo struktura webu se mohly změnit."); }
            var redirect = (string)grid["redirect"];
            if (!string.IsNullOrEmpty(redirect))
            {
                if (new Uri(_origin, redirect).AbsolutePath.StartsWith("/cs/sign/", StringComparison.Ordinal))
                    throw new SessionExpiredException();
                throw new InvalidOperationException("Zásilkovna požaduje neočekávané přesměrování seznamu zásilek.");
            }
            if ((string)grid["_datagrid_name"] != "list")
                throw new InvalidOperationException("Zásilkovna nevrátila očekávaný seznam zásilek.");
            return grid;
        }

        private string Send(Uri uri, Dictionary<string, string> fields = null, bool ajax = false)
        {
            for (var redirects = 0; redirects < 10; redirects++)
            {
                if (uri.Scheme != _origin.Scheme || uri.Host != _origin.Host || uri.Port != _origin.Port)
                    throw new InvalidOperationException("Zásilkovna vrátila přesměrování mimo klientskou sekci.");
                using (var request = new HttpRequestMessage(fields == null ? HttpMethod.Get : HttpMethod.Post, uri))
                {
                    request.Headers.Referrer = _origin;
                    if (ajax) request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    if (fields != null) request.Content = new FormUrlEncodedContent(fields);
                    using (var response = _http.SendAsync(request).GetAwaiter().GetResult())
                    {
                        var code = (int)response.StatusCode;
                        if (code == 301 || code == 302 || code == 303 || code == 307 || code == 308)
                        {
                            if (response.Headers.Location == null)
                                throw new InvalidOperationException("Přesměrování Zásilkovny neobsahuje cílovou adresu.");
                            uri = new Uri(uri, response.Headers.Location);
                            if (code == 301 || code == 302 || code == 303) fields = null;
                            if (ajax && uri.AbsolutePath.StartsWith("/cs/sign/", StringComparison.Ordinal))
                                throw new SessionExpiredException();
                            continue;
                        }
                        if (ajax && (code == 401 || code == 403)) throw new SessionExpiredException();
                        if (!response.IsSuccessStatusCode)
                            throw new InvalidOperationException($"Klientská sekce Zásilkovny vrátila HTTP {code}.");
                        // Do not log credentials, cookies, or response bodies containing customer data.
                        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
            }
            throw new InvalidOperationException("Příliš mnoho přesměrování klientské sekce Zásilkovny.");
        }

        internal static HtmlDocument ParseHtml(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);
            return document;
        }

        public void Dispose() => _http.Dispose();

        private sealed class SessionExpiredException : Exception { }
    }
}
