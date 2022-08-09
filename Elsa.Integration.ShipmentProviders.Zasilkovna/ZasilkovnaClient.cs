using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                                                                        "Expedovat zboží"};

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

        public byte[] GenerateShipmentRequestDocument(IEnumerable<IPurchaseOrder> orders)
        {
            var orderList = orders.ToList();
            m_log.Info($"Zacinam vytvareni dokumentu pro Zasilkovnu, zdroj = {orderList.Count} objednavek");

            using (var stream = new MemoryStream())
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            {
                var generator = new CsvGenerator(streamWriter, ZasilkovnaColIndex);

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

                        var pobockaId = GetPobockaId(order);

                        if (string.IsNullOrWhiteSpace(order.CustomerEmail)
                            && string.IsNullOrWhiteSpace(order.DeliveryAddress?.Phone)
                            && string.IsNullOrWhiteSpace(order.InvoiceAddress?.Phone))
                        {
                            throw new Exception("Musi byt telefon nebo e-mail");
                        }

                        var externalDeliveryProvider = false;
                        if (string.IsNullOrWhiteSpace(pobockaId))
                        {
                            externalDeliveryProvider = true;

                            pobockaId = GetBranches().GetPobockaId(shipmentTitle, GetShipmentMethodsMapping());
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
                            .CellMan(pobockaId) //12 Cílová pobočka
                            .CellMan( /*"biorythme.cz"*/m_config.ClientName) //13 Odesilatel
                            .CellMan(0) //14 Obsah 18+
                            .CellOpt(null) //15 Zpožděný výdej
                            ; 

                        if (externalDeliveryProvider)
                        {

                            generator.CellMan(order.DeliveryAddress?.Street, order.InvoiceAddress?.Street) //16
                                .CellMan(
                                    GetFormattedHouseNumber(order.DeliveryAddress),
                                    GetFormattedHouseNumber(order.InvoiceAddress)) //17
                                .CellMan(order.DeliveryAddress?.City, order.InvoiceAddress?.City) //18
                                .CellMan(order.DeliveryAddress?.Zip, order.InvoiceAddress?.Zip) //19
                                ;
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

        public string GetOrderNumberByPackageNumber(string packageNumber)
        {
            const string orderNoHeader = "Číslo objednávky</th>";

            var url = $"https://www.zasilkovna.cz/vyhledavani/{packageNumber}";

            /*
						<th>Číslo objednávky</th>
						<td>1800529</td>
					*/

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

            return $"{descriptive}/{orientation}";
        }
    }
}
