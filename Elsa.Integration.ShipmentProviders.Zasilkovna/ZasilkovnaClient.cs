using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Elsa.Commerce.Core.Shipment;
using Elsa.Common.Communication;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Integration.ShipmentProviders.Zasilkovna.Model;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    public class ZasilkovnaClient : IShipmentProvider
    {
        private readonly WebFormsClient m_formsClient = new WebFormsClient();

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

        public ZasilkovnaClient(ILog log, ZasilkovnaClientConfig config)
        {
            m_log = log;
            m_config = config;
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

                            pobockaId = GetBranches().GetPobockaId(shipmentTitle);
                        }

                        generator.CellOpt(null) //1
                            .CellMan(order.PreInvoiceId) //2
                            .CellMan(order.DeliveryAddress?.FirstName, order.InvoiceAddress?.FirstName) //3
                            .CellMan(order.DeliveryAddress?.LastName, order.InvoiceAddress?.LastName) //4
                            .CellOpt(
                                externalDeliveryProvider
                                    ? (string.IsNullOrWhiteSpace(order.DeliveryAddress?.CompanyName)
                                           ? order.InvoiceAddress?.CompanyName
                                           : order.DeliveryAddress?.CompanyName)
                                    : string.Empty) //5
                            .CellOpt(order.CustomerEmail) //6
                            .CellOpt(order.DeliveryAddress?.Phone, order.InvoiceAddress?.Phone) //7
                            .CellOpt(dobirkovaCastka) //8
                            .CellMan(order.Currency.Symbol) //9
                            .CellMan(order.PriceWithVat) //10
                            .CellOpt() //11
                            .CellMan(pobockaId) //12
                            .CellMan( /*"biorythme.cz"*/m_config.ClientName) //13
                            .CellMan(0) //14
                            .CellOpt(null) //15
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
