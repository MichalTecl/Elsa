using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Shipment;
using Elsa.Common.Caching;
using Elsa.Common.Communication;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.ShipmentProviders.Zasilkovna.Entities;
using Elsa.Integration.ShipmentProviders.Zasilkovna.Model;
using Robowire.RobOrm.Core;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    public class ZasilkovnaClient : IShipmentProvider
    {
        const string DPD_PICKUP_CODE = "50101";
        const string DPD_PRIVATE_CODE = "40054";

        private readonly WebFormsClient _formsClient;

        private readonly IErpClientFactory _erpClientFactory;
        private readonly IOrderWeightCalculator _orderWeightCalculator;

        public static readonly string[] ZasilkovnaColIndex = new[] {  "Vyhrazeno",
                                                                        "Číslo objednávky",
                                                                        "Jméno",
                                                                        "Příjmení",
                                                                        "Firma",
                                                                        "E-mail",
                                                                        "Mobil",
                                                                        "Dobírková částka",
                                                                        "Měna",
                                                                        "Hodnota zásilky",
                                                                        "Hmotnost zásilky",
                                                                        "Cílová pobočka",
                                                                        "Odesilatel",
                                                                        "Obsah 18+",
                                                                        "Zpožděný výdej",
                                                                        "Dodání poštou - Ulice",
                                                                        "Dodání poštou - Číslo domu",
                                                                        "Dodání poštou - Obec",
                                                                        "Dodání poštou - PSČ",
                                                                        //"Expedovat zboží",
                                                                        "Dodání poštou - Země",
                                                                        "MobilBP",
                                                                        "MobilP",
                                                                        "KontaktniOs",
                                                                        "Dobírka",
                                                                        "DPD_Služba",
                                                                        "DPD_VýdejníMísto"   };

        private BranchesDocument _branches;

        private readonly ILog _log;
        private readonly ZasilkovnaClientConfig _config;
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly ISession _session;

        public ZasilkovnaClient(ILog log, ZasilkovnaClientConfig config, IErpClientFactory erpClientFactory, IDatabase database, ICache cache, ISession session, IOrderWeightCalculator orderWeightCalculator)
        {
            _log = log;
            _config = config;
            _erpClientFactory = erpClientFactory;
            _database = database;
            _cache = cache;
            _session = session;
            _orderWeightCalculator = orderWeightCalculator;
            _formsClient = new WebFormsClient(log);
        }

        public byte[] GenerateShipmentRequestDocument(IEnumerable<IPurchaseOrder> orders, bool uniFormat = false)
        {
            var orderList = orders.ToList();
            _log.Info($"Zacinam vytvareni dokumentu pro Zasilkovnu, zdroj = {orderList.Count} objednavek");

            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            {
                if (!uniFormat)
                {
                    streamWriter.WriteLine("\"verze 4\"");
                    streamWriter.WriteLine();
                }

                var generator = new CsvGenerator(streamWriter, ZasilkovnaColIndex, uniFormat, uniFormat ? ';' : ',');

                foreach (var order in orderList)
                {
                    try
                    {
                        var shipmentTitle = order.ShippingMethodName;

                        var dobirkovaCastka = StringUtil.FormatDecimal(order.PriceWithVat);

                        if (!string.IsNullOrWhiteSpace(dobirkovaCastka) && order.Currency.Symbol == "CZK")
                        {
                            dobirkovaCastka = ((int)double.Parse(dobirkovaCastka)).ToString();
                        }

                        if (!order.IsPayOnDelivery)
                        {
                            dobirkovaCastka = string.Empty;
                        }

                        if (string.IsNullOrWhiteSpace(order.CustomerEmail)
                                && string.IsNullOrWhiteSpace(order.DeliveryAddress?.Phone)
                                && string.IsNullOrWhiteSpace(order.InvoiceAddress?.Phone))
                        {
                            throw new Exception("Musi byt telefon nebo e-mail");
                        }

                        string pobockaId = null;
                        var externalDeliveryProvider = false;
                        if (!uniFormat)
                        {
                            pobockaId = GetPobockaId(order);
                                                                                    
                            if (string.IsNullOrWhiteSpace(pobockaId))
                            {
                                externalDeliveryProvider = true;

                                //pobockaId = GetBranches().GetPobockaId(shipmentTitle, GetShipmentMethodsMapping());
                                pobockaId = GetPobockaId(shipmentTitle, GetShipmentMethodsMapping());
                            }
                        }

                        if (order.ErpId == null)
                        {
                            throw new InvalidOperationException("Unexpected order without Erp");
                        }

                        var erpClient = _erpClientFactory.GetErpClient(order.ErpId.Value);
                        var trackingNumber = erpClient.GetPackingReferenceNumber(order);

                        decimal? weight = null;

                        try
                        {
                            weight = _orderWeightCalculator.GetWeight(order);
                        }
                        catch (Exception e)
                        {
                            _log.Error($"Weight calc failed", e);
                        }
                                                
                        // not sure about Version 4 used by Elsa, but ver. 6 is documented here: https://docs.packetery.com/03-creating-packets/01-csv-import.html
                        generator.CellOpt(null) //1 Vyhrazeno
                            .CellMan(trackingNumber) //2 Číslo objednávky
                            .CellMan(order.DeliveryAddress?.FirstName, order.InvoiceAddress?.FirstName) //3 Jméno
                            .CellMan(order.DeliveryAddress?.LastName, order.InvoiceAddress?.LastName) //4 Příjmení
                            .CellOpt(
                                externalDeliveryProvider
                                    ? (string.IsNullOrWhiteSpace(order.DeliveryAddress?.CompanyName)
                                           ? order.InvoiceAddress?.CompanyName
                                           : order.DeliveryAddress?.CompanyName)
                                    : string.Empty) //5 Firma
                            .CellOpt(order.CustomerEmail) //6 E-mail
                            .CellOpt(order.DeliveryAddress?.Phone, order.InvoiceAddress?.Phone) //7 Mobil
                            .CellOpt(dobirkovaCastka) //8 Dobírková částka
                            .CellMan(order.Currency.Symbol) //9 Měna
                            .CellMan(StringUtil.FormatDecimal(order.PriceWithVat)) //10 Hodnota zásilky
                            .CellOpt(StringUtil.FormatDecimal(weight)) //11 Hmotnost zásilky
                            .Cell(!uniFormat, pobockaId) //12 Cílová pobočka
                            .CellMan(/*"biorythme.cz"*/_config.ClientName) //13 Odesilatel
                            .CellMan(0) //14 Obsah 18+
                            .CellOpt(null) //15 Zpožděný výdej                            
                            ; 

                        if (externalDeliveryProvider || uniFormat)
                        {

                            generator.CellMan(order.DeliveryAddress?.Street, order.InvoiceAddress?.Street) //16
                                .CellMan(
                                    GetFormattedHouseNumber(order.DeliveryAddress),
                                    GetFormattedHouseNumber(order.InvoiceAddress)) //17
                                .CellMan(order.DeliveryAddress?.City, order.InvoiceAddress?.City) //18
                                .CellMan(order.DeliveryAddress?.Zip, order.InvoiceAddress?.Zip) //19
                                .ConditionalCellMan(uniFormat, order.DeliveryAddress?.Country, order.InvoiceAddress?.Country, "cz")
                                ;
                        }

                        //generator.CellOpt(null); //20 Expedovat zbozi

                        if (uniFormat) 
                        {
                            var phone = order.DeliveryAddress?.Phone ?? order.InvoiceAddress?.Phone;
                            var pp = SplitPhonePrefixBody(phone);

                            generator.CellOpt(pp.Item2).CellOpt(pp.Item1);
                            generator.CellMan(order.CustomerName);

                            generator.CellMan(order.IsPayOnDelivery ? "1" : "0");

                            var pickupRef = ParseDpdPickupRef(order.ShippingMethodName);
                                                        
                            generator.CellMan(string.IsNullOrWhiteSpace(pickupRef) ? DPD_PRIVATE_CODE : DPD_PICKUP_CODE);
                            generator.CellOpt(pickupRef);
                        }

                        generator.CommitRow();
                    }
                    catch (Exception ex)
                    {
                        generator.RollbackRow();
                        throw new InvalidOperationException(
                                  "Chyba objednavky " + order.OrderNumber + ":" + ex.Message,
                                  ex);
                    }
                }

                streamWriter.Flush();
                return stream.ToArray();
            }
        }

        private string ParseDpdPickupRef(string shippingMethodName)
        {
            if (string.IsNullOrWhiteSpace(shippingMethodName))
                return null;

            // Česká republika - DPD výdejní místo - (CZ21961) AlzaBox Chrudim (Billa), Rooseveltova 47, 53701, Chr
            var lbr = shippingMethodName.IndexOf('(');
            if (lbr < 0)
                return null;

            shippingMethodName = shippingMethodName.Substring(lbr);
            // (CZ21961) AlzaBox Chrudim (Billa), Rooseveltova 47, 53701, Chr

            var rbr = shippingMethodName.IndexOf(')');
            if (rbr < 1)
                return null;

            shippingMethodName = shippingMethodName.Substring(0, rbr);
            // (CZ21961)

            shippingMethodName = shippingMethodName.Trim(' ', '(', ')');

            if (string.IsNullOrWhiteSpace(shippingMethodName))
                return null;

            return shippingMethodName;
        }

        private string GetPobockaId(string deliveryName, IDictionary<string, string> shipmentMethodsMapping) 
        {
            var originalDeliveryName = deliveryName;

            deliveryName = deliveryName.Trim();

            while (true) 
            {
                string mapped;
                if (!shipmentMethodsMapping.TryGetValue(deliveryName, out mapped))
                    break;

                if (deliveryName == mapped) 
                {
                    _log.Error($"Shipment method mapping loop '{deliveryName}'");
                    break;
                }

                _log.Info($"Appplied shipment method mapping: '{deliveryName}' -> '{mapped}'");
                deliveryName = mapped;
            }

            var index = GetShipMethodIdIndex();
            
            var key = deliveryName.ToLowerInvariant().Trim();
            if (!index.TryGetValue(key, out var id)) 
            {
                _log.Error($"Delivery name was '{originalDeliveryName}', mapped by rules to '{deliveryName}'. Not found in the index by '{key}'. Index.Count={index.Count}");

                throw new Exception(string.Format("Neexistující způsob dopravy \"{0}\"", originalDeliveryName));
            }

            return id;
        }

        private Dictionary<string, string> GetShipMethodIdIndex() 
        {
            return _cache.ReadThrough("zasilkovnaShipMethodIndex", TimeSpan.FromSeconds(10), () => {

                var fromDatabase = _database.SelectFrom<IZasilkovnaShipMapping>().Execute();
                var result = new Dictionary<string, string>();

                foreach (var dbRecord in fromDatabase)
                    result[dbRecord.Name.ToLowerInvariant().Trim()] = dbRecord.ZasilkovnaId;

                var goldSrc = GetBranchIndexFromZasilkovna();

                foreach(var srcRec in goldSrc)
                {
                    if(result.TryGetValue(srcRec.Item1, out var idInDatabase) && (idInDatabase == srcRec.Item2)) 
                    {
                        continue;
                    }

                    SaveShipMethod(srcRec);
                    result[srcRec.Item1] = srcRec.Item2;
                }

                return result;
            });
        }

        private void SaveShipMethod(Tuple<string, string> srcRec)
        {
            var matchingRecs = _database.SelectFrom<IZasilkovnaShipMapping>().Where(m => m.Name == srcRec.Item1).Execute().ToList();
            foreach(var mrec in matchingRecs) 
            {
                _log.Error($"WARN - Detected shipmentId change - '{mrec.Name}' [{mrec.ZasilkovnaId}] -> [{srcRec.Item1}]");
                _database.Delete(mrec);
            }

            var newMap = _database.New<IZasilkovnaShipMapping>();
            newMap.ZasilkovnaId = srcRec.Item2;
            newMap.Name = srcRec.Item1;

            _database.Save(newMap);

            _log.Info($"Zasilkova Shipment Method index added: {newMap.ZasilkovnaId} : {newMap.Name}");
        }

        private IEnumerable<Tuple<string, string>> GetBranchIndexFromZasilkovna() 
        {            
            var branches = GetBranches().BranchesList.Branches;
            foreach(var branch in branches) 
            {
                var n = branch.Name.Trim().ToLowerInvariant();
                var l = branch.LabelName.Trim().ToLowerInvariant();

                yield return new Tuple<string, string>($"{n} {branch.Id}", branch.Id);

                yield return new Tuple<string, string>(n, branch.Id);

                if (n != l)
                    yield return new Tuple<string, string>(l, branch.Id);
            }
        }

        public string GetOrderNumberByPackageNumber(string packageNumber)
        {
            const string orderNoHeader = "Číslo objednávky</th>";

            var url = $"https://www.zasilkovna.cz/vyhledavani/{packageNumber}";

            /*
						<th>Číslo objednávky</th>
						<td>1800529</td>
					*/

            try
            {
                var html = _formsClient.GetString(url);

                var thIndex = html.IndexOf(orderNoHeader, StringComparison.Ordinal);
                if (thIndex < 0)
                {
                    return null;
                }

                html = html.Substring(thIndex + orderNoHeader.Length + 1).Trim();
                html = html.Substring(0, 100);
                html = html.Replace("<td>", string.Empty);

                var cIndex = html.IndexOf("</td>", StringComparison.Ordinal);
                html = html.Substring(0, cIndex).Trim();

                return html;
            }
            catch(WebException ex)
            {                
                _log.Error("Chyba při hledání čísla zásilky v Zásilkovně", ex);
                return null;
            }
        }

        public void SetShipmentMethodsMapping(Dictionary<string, string> mapping)
        {
            try
            {
                using (var tx = _database.OpenTransaction())
                {
                    _database.DeleteFrom<IShipmentMethodMapping>(q => q.Where(m => m.ProjectId == _session.Project.Id));

                    foreach (var map in mapping)
                    {
                        var entity = _database.New<IShipmentMethodMapping>();
                        entity.Source = map.Key.Trim();
                        entity.Target = map.Value.Trim();
                        entity.ProjectId = _session.Project.Id;

                        _database.Save(entity);
                    }

                    tx.Commit();
                }
                
            }
            finally
            {
                _cache.Remove($"shipmentMethodsMap_{_session.Project.Id}");
            }
        }

        public Dictionary<string, string> GetShipmentMethodsMapping()
        {
            return _cache.ReadThrough($"shipmentMethodsMap_{_session.Project.Id}", TimeSpan.FromHours(1), () =>
            {
                return _database.SelectFrom<IShipmentMethodMapping>()
                    .Where(m => m.ProjectId == _session.Project.Id).Execute().ToDictionary(m => m.Source, m => m.Target);
            });
        }

        private BranchesDocument GetBranches()
        {
            return _branches ?? (_branches = BranchesDocument.DownloadV5(_log, _config.ApiToken));
        }

        private static string GetPobockaId(IPurchaseOrder order)
        {
            const string pobNumStart = "ZÁSILKOVNA - (";

            //Česká republika - ZÁSILKOVNA - (1339) Pardubice, U Afi paláce, Palackého 1932

            if (string.IsNullOrWhiteSpace(order.ShippingMethodName))
            {
                return null;
            }

            var a = order.ShippingMethodName.IndexOf(pobNumStart, StringComparison.InvariantCultureIgnoreCase);
            if (a == -1)
            {
                return null;
            }

            var textParts = order.ShippingMethodName.Substring(a + pobNumStart.Length).Split(new[] {"(", ")"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pt in textParts)
            {
                int pobNum;
                if (int.TryParse(pt.Trim(), out pobNum))
                {
                    return pobNum.ToString();
                }
            }

            return null;
        }

        private static string GetFormattedHouseNumber(IAddress address)
        {
            if (address == null)
            {
                return null;
            }

            return FormatHouseNumber(address.OrientationNumber, address.DescriptiveNumber);
        }

        private static string FormatHouseNumber(string orientation, string descriptive)
        {
            if (string.IsNullOrWhiteSpace(orientation))
            {
                return descriptive;
            }

            if (string.IsNullOrWhiteSpace(descriptive))
            {
                return orientation;
            }

            if (descriptive.Contains("/"))
                return descriptive;

            if (orientation.Contains("/"))
                return orientation;

            return $"{descriptive}/{orientation}";
        }

        public IEnumerable<string> GetShipmentMethodsList()
        {
            var branches = GetBranches();
            foreach(var branch in branches.BranchesList.Branches)
            {
                yield return branch.Name;

                if (branch.Name != branch.LabelName)
                    yield return branch.LabelName;
            }
        }

        private static Tuple<string, string> SplitPhonePrefixBody(string phone) 
        {
            // +420775154809
            var prefix = string.Empty;
            var body = string.Empty;

            if (!string.IsNullOrWhiteSpace(phone)) 
            {
                phone = phone.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("/", string.Empty);

                body = phone;
                if (body.Length >= 9) 
                {
                    body = body.Substring(body.Length - 9);
                    prefix = phone.Substring(0, phone.Length - 9);
                }
            }

            if (prefix.Length > 4 && prefix.StartsWith("00"))
                prefix = $"+{prefix.Substring(2)}";

            return new Tuple<string, string>(prefix, body);
        }
    }
}
