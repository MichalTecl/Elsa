using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.CommonData.ExcelInterop;
using Elsa.Apps.Invoices.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common.Utils;
using Robowire.RobOrm.Core;

namespace Elsa.Apps.Invoices
{
    public class InvoiceModelProcessor : IInvoiceFileProcessor
    {
        private readonly IDatabase _database;
        private readonly ISupplierRepository _supplierRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IMaterialBatchRepository _batchRepository;
        private readonly IMaterialRepository _materialRepository;
        private readonly IUnitRepository _unitRepository;
        private readonly IMaterialFacade _materialFacade;

        public InvoiceModelProcessor(IDatabase database, ISupplierRepository supplierRepository, ICurrencyRepository currencyRepository, IMaterialBatchRepository batchRepository, IMaterialRepository materialRepository, IUnitRepository unitRepository, IMaterialFacade materialFacade)
        {
            _database = database;
            _supplierRepository = supplierRepository;
            _currencyRepository = currencyRepository;
            _batchRepository = batchRepository;
            _materialRepository = materialRepository;
            _unitRepository = unitRepository;
            _materialFacade = materialFacade;
        }

        public void ProcessFile(InvoiceModel model)
        {
            if (model == null)
            {
                throw new InvalidOperationException("Invalid model");
            }

            var now = DateTime.Now;
            if (!(model.Date.Year == now.Year && model.Date.Month == now.Month))
                throw new Exception("Lze naskladnit pouze v aktuálním měsíci");

            var supplier = _supplierRepository.GetSupplier(model.SupplierName ?? string.Empty).Ensure($"Neexistující dodavatel \"{model.SupplierName}\"");
            var currency = _currencyRepository.GetCurrency(model.Currency).Ensure($"Neexistující symbol měny \"{model.Currency}\"");
            
            if (string.IsNullOrWhiteSpace(model.InvoiceNumber))
            {
                throw new InvalidOperationException("Chybí číslo faktury");
            }

            var validItems = model.Items.Where(i => !string.IsNullOrWhiteSpace(i.MaterialName)).ToList();
            if (!validItems.Any()) 
            {
                throw new InvalidOperationException("Žádné položky");
            }

            var noPriced = validItems.FirstOrDefault(i => i.Price < 0.0001m || i.Quantity < 0.0001m);
            if (noPriced != null)
            {
                throw new InvalidOperationException($"Položka \"{noPriced.MaterialName}\" nemá cenu nebo mnozstvi");
            }
                        
            var itemsTotal = validItems.Sum(i => i.Price);
            var pdiff = Math.Abs((itemsTotal + model.ShipmentPrice) - model.TotalPrice);
            if (pdiff > 0.1m)
                throw new InvalidOperationException($"Součet cen položek + cena dopravy neodpovídá celkové ceně");


            decimal priceFactor = model.TotalPrice / validItems.Sum(i => i.Price);
            
            var itemsWithCorrectedPrices = new List<Tuple<InvoiceItem, decimal>>(validItems.Count);

            foreach (var item in validItems)
            {
                itemsWithCorrectedPrices.Add(new Tuple<InvoiceItem, decimal>(item, item.Price * priceFactor));
            }
            
            using (var tx = _database.OpenTransaction())
            {
                var existingBatches = _batchRepository.GetBatchesByInvoiceNumber(model.InvoiceNumber, supplier.Id).ToList();
                
                foreach (var itemWithCorrectedPrice in itemsWithCorrectedPrices)
                {
                    var invoiceItem = itemWithCorrectedPrice.Item1;

                    var material = _materialRepository.GetMaterialByName(invoiceItem.MaterialName)
                        .Ensure($"Neznámý materiál \"{invoiceItem.MaterialName}\"");

                    var unit = _unitRepository.GetUnitBySymbol(invoiceItem.Unit)
                        .Ensure($"Neznámá jednotka \"{invoiceItem.Unit}");



                    if (invoiceItem.Id > 0)
                    {
                        throw new NotSupportedException($"Toto jeste neni podporovano :(");

                        //var existing = existingBatches.FirstOrDefault(b => b.Id == invoiceItem.Id);
                        //if (existing == null)
                        //{
                        //    throw new InvalidOperationException($"Soubor obsahuje polozku s id={invoiceItem.Id}. Toto ID sarze ale nebylo v databazi nalezeno");
                        //}

                        //existingBatches.Remove(existing);

                        //_batchRepository.UpdateBatch(invoiceItem.Id, b =>
                        //{
                        //    b.BatchNumber = invoiceItem.BatchNumber;
                        //    b.Created = receiveDate;
                        //    b.InvoiceNr = model.InvoiceNumber;
                        //    b.InvoiceVarSymbol = model.VarSymbol;
                        //    b.Price = itemWithCorrectedPrice.Item2;
                        //    b.
                        //});
                    }

                    var batchNumber = invoiceItem.BatchNumber?.Trim();

                    if (string.IsNullOrWhiteSpace(batchNumber))
                    {
                        var materialInfo = _materialFacade.GetMaterialInfo(invoiceItem.MaterialName);
                        if (materialInfo == null)
                        {
                            throw new InvalidOperationException($"Neznamy material {invoiceItem.MaterialName}");
                        }

                        if (!materialInfo.AutomaticBatches)
                        {
                            throw new InvalidOperationException($"Pro materiál \"{invoiceItem.MaterialName}\" musí být uvedeno čslo šarže");
                        }

                        batchNumber = materialInfo.AutoBatchNr;
                    }

                    if ((batchNumber?.Trim().Length ?? 0) < 3)
                    {
                        throw new InvalidOperationException($"Číslo šarže pro materiál \"{invoiceItem.MaterialName}\" musí mít alespoň tři znaky.");
                    }

                    _batchRepository.SaveBottomLevelMaterialBatch(0,
                        material.Adaptee, 
                        invoiceItem.Quantity, 
                        unit,
                        batchNumber, 
                        model.Date, 
                        itemWithCorrectedPrice.Item2, 
                        model.InvoiceNumber,
                        supplier.Name, 
                        currency.Symbol, 
                        model.VarSymbol);
                }    

                tx.Commit();
            }
        }


        
        
    }
}
