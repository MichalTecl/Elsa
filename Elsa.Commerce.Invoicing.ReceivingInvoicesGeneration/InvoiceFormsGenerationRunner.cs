using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Commerce.Core.Repositories;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;

using RobOrm.Core;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration
{
    public class InvoiceFormsGenerationRunner : IInvoiceFormsGenerationRunner
    {
        private readonly IDatabase _database;
        private readonly IMaterialRepository _materialRepository;
        private readonly ILog _log;
        private readonly IInvoiceFormGeneratorFactory _generatorFactory;
        private readonly IInvoiceFormsRepository _invoiceFormsRepository;
        private readonly ISession _session;
        private readonly IFixedCostRepository _fixedCostRepository;
        
        public InvoiceFormsGenerationRunner(IDatabase database,
            ILog log,
            IMaterialRepository materialRepository,
            IInvoiceFormGeneratorFactory generatorFactory,
            IInvoiceFormsRepository invoiceFormsRepository,
            ISession session, IFixedCostRepository fixedCostRepository)
        {
            _database = database;
            _log = log;
            _materialRepository = materialRepository;
            _generatorFactory = generatorFactory;
            _invoiceFormsRepository = invoiceFormsRepository;
            _session = session;
            _fixedCostRepository = fixedCostRepository;
        }

        public IInvoiceFormGenerationContext RunReceivingInvoicesGeneration(int formTypeId, int year, int month)
        {
            _log.Info($"Called for month={month}, year={year}");

            _fixedCostRepository.CalculateFixedCostComponents(year, month);

            var context = PrepareContext(formTypeId, year, month);

            foreach (var inventory in _materialRepository.GetMaterialInventories())
            {
                if (string.IsNullOrWhiteSpace(inventory.ReceivingInvoiceFormGeneratorName))
                {
                    context.Info($"Pro sklad {inventory.Name} neni nastaveno generovani prijemek, preskakuji");
                    continue;
                }

                try
                {
                    DoGeneration(inventory, context, year, month, null);
                }
                catch (Exception ex)
                {
                    context.Error($"Generovani prijemek pro sklad {inventory.Name} selhalo: {ex.Message}", ex);
                }
            }

            if (context.CountForms() == 0)
            {
                context.Error($"Nebyla vygenerována žádná příjemka");
            }

            return context;
        }

        private IInvoiceFormGenerationContext PrepareContext(int formTypeId, int year, int month)
        {
            var existingCollection = _invoiceFormsRepository.FindCollection(formTypeId, year, month);

            if (existingCollection?.ApproveUserId != null)
            {
                throw new InvalidOperationException("Jiz bylo vygenerovano");
            }

            var preapprovedMessages = new HashSet<string>();

            if (existingCollection != null)
            {
                foreach (var m in existingCollection.Log.Where(l =>
                    l.IsWarning && (l.ApproveUserId == _session.User.Id)))
                {
                    preapprovedMessages.Add(m.Message);
                }

                _invoiceFormsRepository.DeleteCollection(existingCollection.Id);
            }

            var context = _invoiceFormsRepository.StartGeneration($"{month.ToString().PadLeft(2, '0')}/{year}",
                year,
                month,
                formTypeId);
            context.AutoApproveWarnings(preapprovedMessages);
            return context;
        }

        public IInvoiceFormGenerationContext RunTasks(int year, int month)
        {
            _fixedCostRepository.CalculateFixedCostComponents(year, month);

            var invoiceFormType = _invoiceFormsRepository.GetInvoiceFormTypes()
                .FirstOrDefault(t => t.GeneratorName == "ReleasingForm");
            if (invoiceFormType == null)
            {
                throw new InvalidOperationException("InvoiceFormType was not found by GeneratorName 'ReleasingForm'");
            }

            _log.Info($"Called for month={month}, year={year}");
            
            //using (var tx = _database.OpenTransaction())
            //{
                var context = PrepareContext(invoiceFormType.Id, year, month);
                var allGenerationTasks = _invoiceFormsRepository.GetReleasingFormsTasks();

                foreach (var task in allGenerationTasks)
                {
                    var inventories = task.Inventories?.Select(i => i.MaterialInventoryId).ToList();
                    if (inventories?.Any() != true)
                    {
                        inventories = _materialRepository.GetMaterialInventories().Select(i => i.Id).ToList();
                    }
                    
                    foreach (var inventoryId in inventories)
                    {
                        var materialInventory = _materialRepository.GetMaterialInventories()
                            .FirstOrDefault(i => i.Id == inventoryId);

                        if (materialInventory == null)
                        {
                            throw new InvalidOperationException("Unknown inventory Id");
                        }

                        try
                        {
                            context.Info($"Začínám generování výdejek typu \"{task.FormText}\" ze skladu {materialInventory.Name}");
                            DoGeneration(materialInventory, context, year, month, task);
                            context.Info($"Dokončeno generování výdejek typu \"{task.FormText}\" ze skladu {materialInventory.Name}");
                        }
                        catch (Exception ex)
                        {
                            context.Error($"Generovani vydejek typu \"{task.FormText}\" pro sklad {materialInventory.Name} selhalo: {ex.Message}", ex);
                        }
                    }
                }
                
                if (context.CountForms() == 0)
                {
                    context.Error($"Nebyla vygenerována žádná výdejka");
                }

                //tx.Commit();

                return context;
            //}
        }

        private void DoGeneration(IMaterialInventory inventory, IInvoiceFormGenerationContext context, int year, int month, IReleasingFormsGenerationTask task)
        {
            var generator = _generatorFactory.Get(task == null ? inventory.ReceivingInvoiceFormGeneratorName : task.GeneratorName);

            if (task == null)
            {
                context.Info($"Zacinam generovat: {generator.GetGenerationName(inventory, year, month)}");
            }

            generator.Generate(inventory, year, month, context, task);
        }
    }
}
