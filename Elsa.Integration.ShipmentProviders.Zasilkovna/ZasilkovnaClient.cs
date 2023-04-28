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
        private readonly WebFormsClient m_formsClient;

        private readonly IErpClientFactory m_erpClientFactory;
        private readonly IOrderWeightCalculator m_orderWeightCalculator;

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
                                                                        "MobilP" };

        private BranchesDocument m_branches;

        private readonly ILog m_log;
        private readonly ZasilkovnaClientConfig m_config;
        private readonly IDatabase m_database;
        private readonly ICache m_cache;
        private readonly ISession m_session;

        public ZasilkovnaClient(ILog log, ZasilkovnaClientConfig config, IErpClientFactory erpClientFactory, IDatabase database, ICache cache, ISession session, IOrderWeightCalculator orderWeightCalculator)
        {
            m_log = log;
            m_config = config;
            m_erpClientFactory = erpClientFactory;
            m_database = database;
            m_cache = cache;
            m_session = session;
            m_orderWeightCalculator = orderWeightCalculator;
            m_formsClient = new WebFormsClient(log);
        }

        public byte[] GenerateShipmentRequestDocument(IEnumerable<IPurchaseOrder> orders, bool uniFormat = false)
        {
            var orderList = orders.ToList();
            m_log.Info($"Zacinam vytvareni dokumentu pro Zasilkovnu, zdroj = {orderList.Count} objednavek");

            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            {
                if (!uniFormat)
                {
                    streamWriter.WriteLine("\"verze 4\"");
                    streamWriter.WriteLine();
                }

                var generator = new CsvGenerator(streamWriter, ZasilkovnaColIndex, uniFormat);

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

                        var erpClient = m_erpClientFactory.GetErpClient(order.ErpId.Value);
                        var trackingNumber = erpClient.GetPackingReferenceNumber(order);

                        decimal? weight = null;

                        try
                        {
                            weight = m_orderWeightCalculator.GetWeight(order);
                        }
                        catch (Exception e)
                        {
                            m_log.Error($"Weight calc failed", e);
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
                            .CellMan(/*"biorythme.cz"*/m_config.ClientName) //13 Odesilatel
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
                    m_log.Error($"Shipment method mapping loop '{deliveryName}'");
                    break;
                }

                m_log.Info($"Appplied shipment method mapping: '{deliveryName}' -> '{mapped}'");
                deliveryName = mapped;
            }

            var index = GetShipMethodIdIndex();
            
            var key = deliveryName.ToLowerInvariant().Trim();
            if (!index.TryGetValue(key, out var id)) 
            {
                m_log.Error($"Delivery name was '{originalDeliveryName}', mapped by rules to '{deliveryName}'. Not found in the index by '{key}'. Index.Count={index.Count}");

                throw new Exception(string.Format("Neexistující způsob dopravy \"{0}\"", originalDeliveryName));
            }

            return id;
        }

        private Dictionary<string, string> GetShipMethodIdIndex() 
        {
            return m_cache.ReadThrough("zasilkovnaShipMethodIndex", TimeSpan.FromSeconds(10), () => {

                var fromDatabase = m_database.SelectFrom<IZasilkovnaShipMapping>().Execute();
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
            var matchingRecs = m_database.SelectFrom<IZasilkovnaShipMapping>().Where(m => m.Name == srcRec.Item1).Execute().ToList();
            foreach(var mrec in matchingRecs) 
            {
                m_log.Error($"WARN - Detected shipmentId change - '{mrec.Name}' [{mrec.ZasilkovnaId}] -> [{srcRec.Item1}]");
                m_database.Delete(mrec);
            }

            var newMap = m_database.New<IZasilkovnaShipMapping>();
            newMap.ZasilkovnaId = srcRec.Item2;
            newMap.Name = srcRec.Item1;

            m_database.Save(newMap);

            m_log.Info($"Zasilkova Shipment Method index added: {newMap.ZasilkovnaId} : {newMap.Name}");
        }

        private IEnumerable<Tuple<string, string>> GetBranchIndexFromZasilkovna() 
        {            
            var branches = GetBranches().BranchesList.Branches;
            foreach(var branch in branches) 
            {
                var n = branch.Name.Trim().ToLowerInvariant();
                var l = branch.LabelName.Trim().ToLowerInvariant();

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
                var html = m_formsClient.GetString(url);

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
                m_log.Error("Chyba při hledání čísla zásilky v Zásilkovně", ex);
                return null;
            }
        }

        public void SetShipmentMethodsMapping(Dictionary<string, string> mapping)
        {
            try
            {
                using (var tx = m_database.OpenTransaction())
                {
                    m_database.DeleteFrom<IShipmentMethodMapping>(q => q.Where(m => m.ProjectId == m_session.Project.Id));

                    foreach (var map in mapping)
                    {
                        var entity = m_database.New<IShipmentMethodMapping>();
                        entity.Source = map.Key.Trim();
                        entity.Target = map.Value.Trim();
                        entity.ProjectId = m_session.Project.Id;

                        m_database.Save(entity);
                    }

                    tx.Commit();
                }
                
            }
            finally
            {
                m_cache.Remove($"shipmentMethodsMap_{m_session.Project.Id}");
            }
        }

        public Dictionary<string, string> GetShipmentMethodsMapping()
        {
            return m_cache.ReadThrough($"shipmentMethodsMap_{m_session.Project.Id}", TimeSpan.FromHours(1), () =>
            {
                return m_database.SelectFrom<IShipmentMethodMapping>()
                    .Where(m => m.ProjectId == m_session.Project.Id).Execute().ToDictionary(m => m.Source, m => m.Target);
            });
        }

        private BranchesDocument GetBranches()
        {
            return m_branches ?? (m_branches = BranchesDocument.Download(m_config.ApiToken));
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
