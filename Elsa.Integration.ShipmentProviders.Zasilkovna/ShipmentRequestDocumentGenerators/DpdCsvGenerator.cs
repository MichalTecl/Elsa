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

namespace Elsa.Integration.ShipmentProviders.Zasilkovna.ShipmentRequestDocumentGenerators
{
    public class DpdCsvGenerator : IShipmentRequestDocumentGenerator
    {
        const string DPD_PICKUP_CODE = "50101";
        const string DPD_PRIVATE_CODE = "40054";

        private readonly ILog _log;
        private readonly IErpClientFactory _erpClientFactory;
        private readonly IOrderWeightCalculator _orderWeightCalculator;
        private readonly ZasilkovnaClientConfig _config;

        public static readonly string[] ColumnIndex = new[] {  "Vyhrazeno",
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

        public DpdCsvGenerator(ILog log, IErpClientFactory erpClientFactory, IOrderWeightCalculator orderWeightCalculator, ZasilkovnaClientConfig config)
        {
            _log = log;
            _erpClientFactory = erpClientFactory;
            _orderWeightCalculator = orderWeightCalculator;
            _config = config;
        }

        public string Symbol => "DPD";

        public void Generate(List<IPurchaseOrder> orderList, StreamWriter streamWriter, out string fileName)
        {
            fileName = $"DPD_{DateTime.Now:ddMMyyyy}.csv";

            _log.Info($"Zacinam vytvareni dokumentu pro DPD, zdroj = {orderList.Count} objednavek");

            var generator = new CsvGenerator(streamWriter, ColumnIndex, true, ';');

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
                        .Cell(false, pobockaId) //12 Cílová pobočka
                        .CellMan(/*"biorythme.cz"*/_config.ClientName) //13 Odesilatel
                        .CellMan(0) //14 Obsah 18+
                        .CellOpt(null) //15 Zpožděný výdej                            
                        .CellMan(order.DeliveryAddress?.Street, order.InvoiceAddress?.Street) //16
                            .CellMan(
                                ShipmentDataHelper.GetFormattedHouseNumber(order.DeliveryAddress),
                                ShipmentDataHelper.GetFormattedHouseNumber(order.InvoiceAddress)) //17
                            .CellMan(order.DeliveryAddress?.City, order.InvoiceAddress?.City) //18
                            .CellMan(order.DeliveryAddress?.Zip, order.InvoiceAddress?.Zip) //19
                            .CellMan(order.DeliveryAddress?.Country, order.InvoiceAddress?.Country, "cz")
                            ;

                    var phone = order.DeliveryAddress?.Phone ?? order.InvoiceAddress?.Phone;
                    var pp = ShipmentDataHelper.SplitPhonePrefixBody(phone);

                    generator.CellOpt(pp.Item2).CellOpt(pp.Item1);
                    generator.CellMan(order.CustomerName);

                    generator.CellMan(order.IsPayOnDelivery ? "1" : "0");

                    var pickupRef = ParseDpdPickupRef(order.ShippingMethodName);

                    generator.CellMan(string.IsNullOrWhiteSpace(pickupRef) ? DPD_PRIVATE_CODE : DPD_PICKUP_CODE);
                    generator.CellOpt(pickupRef);

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
    }
}
