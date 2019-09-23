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
        private readonly IDatabase m_database;
        private readonly ISupplierRepository m_supplierRepository;
        private readonly ICurrencyRepository m_currencyRepository;
        private readonly IMaterialBatchRepository m_batchRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly IMaterialFacade m_materialFacade;

        public InvoiceModelProcessor(IDatabase database, ISupplierRepository supplierRepository, ICurrencyRepository currencyRepository, IMaterialBatchRepository batchRepository, IMaterialRepository materialRepository, IUnitRepository unitRepository, IMaterialFacade materialFacade)
        {
            m_database = database;
            m_supplierRepository = supplierRepository;
            m_currencyRepository = currencyRepository;
            m_batchRepository = batchRepository;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_materialFacade = materialFacade;
        }

        public void ProcessFile(InvoiceModel model)
        {
            if (model == null)
            {
                throw new InvalidOperationException("Invalid model");
            }

            var supplier = m_supplierRepository.GetSupplier(model.SupplierName ?? string.Empty).Ensure($"Neexistující dodavatel \"{model.SupplierName}\"");
            var currency = m_currencyRepository.GetCurrency(model.Currency).Ensure($"Neexistující symbol měny \"{model.Currency}\"");

            if (!DateTime.TryParseExact(model.Date, ElsaExcelModelBase.ExcelDateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var receiveDate))
            {
                throw new InvalidOperationException($"Chybný formát data \"{model.Date}\"");
            }

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

            decimal priceFactor = model.TotalPrice / validItems.Sum(i => i.Price);
            
            var itemsWithCorrectedPrices = new List<Tuple<InvoiceItem, decimal>>(validItems.Count);

            foreach (var item in validItems)
            {
                itemsWithCorrectedPrices.Add(new Tuple<InvoiceItem, decimal>(item, item.Price * priceFactor));
            }
            
            using (var tx = m_database.OpenTransaction())
            {
                var existingBatches = m_batchRepository.GetBatchesByInvoiceNumber(model.InvoiceNumber, supplier.Id).ToList();
                
                foreach (var itemWithCorrectedPrice in itemsWithCorrectedPrices)
                {
                    var invoiceItem = itemWithCorrectedPrice.Item1;

                    var material = m_materialRepository.GetMaterialByName(invoiceItem.MaterialName)
                        .Ensure($"Neznámý materiál \"{invoiceItem.MaterialName}\"");

                    var unit = m_unitRepository.GetUnitBySymbol(invoiceItem.Unit)
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

                        //m_batchRepository.UpdateBatch(invoiceItem.Id, b =>
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
                        var materialInfo = m_materialFacade.GetMaterialInfo(invoiceItem.MaterialName);
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

                    m_batchRepository.SaveBottomLevelMaterialBatch(0, 
                        material.Adaptee, 
                        invoiceItem.Quantity, 
                        unit,
                        batchNumber, 
                        receiveDate, 
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
