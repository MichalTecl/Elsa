using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Shipment;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna.ShipmentRequestDocumentGenerators
{
    public class Zasilkovna4CsvGenerator : IShipmentRequestDocumentGenerator
    {
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

        private readonly ILog _log;
        private readonly IErpClientFactory _erpClientFactory;
        private readonly IOrderWeightCalculator _orderWeightCalculator;
        private readonly ZasilkovnaClientConfig _config;
        private readonly ZasilkovnaClient _zasilkovna;

        public string Symbol => "zasilkovna";

        public Zasilkovna4CsvGenerator(ILog log, IErpClientFactory erpClientFactory, IOrderWeightCalculator orderWeightCalculator, ZasilkovnaClientConfig config, ZasilkovnaClient zasilkovna)
        {
            _log = log;
            _erpClientFactory = erpClientFactory;
            _orderWeightCalculator = orderWeightCalculator;
            _config = config;
            _zasilkovna = zasilkovna;
        }

        public void Generate(List<IPurchaseOrder> orderList, StreamWriter streamWriter, out string fileName)
        {
            fileName = $"zasilkovna_{DateTime.Now:ddMMyyyy}.csv";

            _log.Info($"Zacinam vytvareni dokumentu pro Zasilkovnu (ver. 4), zdroj = {orderList.Count} objednavek");

            streamWriter.WriteLine("\"verze 4\"");
            streamWriter.WriteLine();

            var generator = new CsvGenerator(streamWriter, ZasilkovnaColIndex, false, ',');

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
                    
                    pobockaId = GetPobockaId(order);

                    if (string.IsNullOrWhiteSpace(pobockaId))
                    {
                        externalDeliveryProvider = true;

                        //pobockaId = GetBranches().GetPobockaId(shipmentTitle, GetShipmentMethodsMapping());
                        pobockaId = _zasilkovna.GetPobockaId(shipmentTitle, _zasilkovna.GetShipmentMethodsMapping());
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

                        if (weight == null)
                            _log.Error($"Weight == null for order Id={order.Id}");

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
                        .CellOpt(StringUtil.FormatDecimal(weight ?? 1M, 2)) //11 Hmotnost zásilky
                        .CellMan(pobockaId) //12 Cílová pobočka
                        .CellMan(/*"biorythme.cz"*/_config.ClientName) //13 Odesilatel
                        .CellMan(0) //14 Obsah 18+
                        .CellOpt(null) //15 Zpožděný výdej                            
                        ;

                    if (externalDeliveryProvider)
                    {

                        generator.CellMan(order.DeliveryAddress?.Street, order.InvoiceAddress?.Street) //16
                            .CellMan(
                                ShipmentDataHelper.GetFormattedHouseNumber(order.DeliveryAddress),
                                ShipmentDataHelper.GetFormattedHouseNumber(order.InvoiceAddress)) //17
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

            var textParts = order.ShippingMethodName.Substring(a + pobNumStart.Length).Split(new[] { "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
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
    }
}
