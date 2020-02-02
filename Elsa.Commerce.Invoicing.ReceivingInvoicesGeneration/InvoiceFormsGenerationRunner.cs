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

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Invoicing.ReceivingInvoicesGeneration
{
    public class InvoiceFormsGenerationRunner : IInvoiceFormsGenerationRunner
    {
        private readonly IDatabase m_database;
        private readonly IMaterialRepository m_materialRepository;
        private readonly ILog m_log;
        private readonly IInvoiceFormGeneratorFactory m_generatorFactory;
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;
        private readonly ISession m_session;
        private readonly IFixedCostRepository m_fixedCostRepository;
        
        public InvoiceFormsGenerationRunner(IDatabase database,
            ILog log,
            IMaterialRepository materialRepository,
            IInvoiceFormGeneratorFactory generatorFactory,
            IInvoiceFormsRepository invoiceFormsRepository,
            ISession session, IFixedCostRepository fixedCostRepository)
        {
            m_database = database;
            m_log = log;
            m_materialRepository = materialRepository;
            m_generatorFactory = generatorFactory;
            m_invoiceFormsRepository = invoiceFormsRepository;
            m_session = session;
            m_fixedCostRepository = fixedCostRepository;
        }

        public IInvoiceFormGenerationContext RunReceivingInvoicesGeneration(int formTypeId, int year, int month)
        {
            m_log.Info($"Called for month={month}, year={year}");

            m_fixedCostRepository.CalculateFixedCostComponents(year, month);

            var context = PrepareContext(formTypeId, year, month);

            foreach (var inventory in m_materialRepository.GetMaterialInventories())
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
            var existingCollection = m_invoiceFormsRepository.FindCollection(formTypeId, year, month);

            if (existingCollection?.ApproveUserId != null)
            {
                throw new InvalidOperationException("Jiz bylo vygenerovano");
            }

            var preapprovedMessages = new HashSet<string>();

            if (existingCollection != null)
            {
                foreach (var m in existingCollection.Log.Where(l =>
                    l.IsWarning && (l.ApproveUserId == m_session.User.Id)))
                {
                    preapprovedMessages.Add(m.Message);
                }

                m_invoiceFormsRepository.DeleteCollection(existingCollection.Id);
            }

            var context = m_invoiceFormsRepository.StartGeneration($"{month.ToString().PadLeft(2, '0')}/{year}",
                year,
                month,
                formTypeId);
            context.AutoApproveWarnings(preapprovedMessages);
            return context;
        }

        public IInvoiceFormGenerationContext RunTasks(int year, int month)
        {
            m_fixedCostRepository.CalculateFixedCostComponents(year, month);

            var invoiceFormType = m_invoiceFormsRepository.GetInvoiceFormTypes()
                .FirstOrDefault(t => t.GeneratorName == "ReleasingForm");
            if (invoiceFormType == null)
            {
                throw new InvalidOperationException("InvoiceFormType was not found by GeneratorName 'ReleasingForm'");
            }

            m_log.Info($"Called for month={month}, year={year}");
            
            //using (var tx = m_database.OpenTransaction())
            //{
                var context = PrepareContext(invoiceFormType.Id, year, month);
                var allGenerationTasks = m_invoiceFormsRepository.GetReleasingFormsTasks();

                foreach (var task in allGenerationTasks)
                {
                    var inventories = task.Inventories?.Select(i => i.MaterialInventoryId).ToList();
                    if (inventories?.Any() != true)
                    {
                        inventories = m_materialRepository.GetMaterialInventories().Select(i => i.Id).ToList();
                    }
                    
                    foreach (var inventoryId in inventories)
                    {
                        var materialInventory = m_materialRepository.GetMaterialInventories()
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
            var generator = m_generatorFactory.Get(task == null ? inventory.ReceivingInvoiceFormGeneratorName : task.GeneratorName);

            if (task == null)
            {
                context.Info($"Zacinam generovat: {generator.GetGenerationName(inventory, year, month)}");
            }

            generator.Generate(inventory, year, month, context, task);
        }
    }
}
