using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Logging;
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
        
        public InvoiceFormsGenerationRunner(IDatabase database,
            ILog log,
            IMaterialRepository materialRepository,
            IInvoiceFormGeneratorFactory generatorFactory,
            IInvoiceFormsRepository invoiceFormsRepository,
            ISession session)
        {
            m_database = database;
            m_log = log;
            m_materialRepository = materialRepository;
            m_generatorFactory = generatorFactory;
            m_invoiceFormsRepository = invoiceFormsRepository;
            m_session = session;
        }

        public IInvoiceFormGenerationContext Run(int formTypeId, int year, int month)
        {
            m_log.Info($"Called for month={month}, year={year}");
            
            using (var tx = m_database.OpenTransaction())
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

                foreach (var inventory in m_materialRepository.GetMaterialInventories())
                {
                    if (string.IsNullOrWhiteSpace(inventory.ReceivingInvoiceFormGeneratorName))
                    {
                        context.Info($"Pro sklad {inventory.Name} neni nastaveno generovani prijemek, preskakuji");
                        continue;
                    }

                    try
                    {
                        DoGeneration(inventory, context, year, month);
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

                tx.Commit();

                return context;
            }
        }

        private void DoGeneration(IMaterialInventory inventory, IInvoiceFormGenerationContext context, int year, int month)
        {
            var generator = m_generatorFactory.Get(inventory.ReceivingInvoiceFormGeneratorName);

            context.Info($"Zacinam generovat: {generator.GetGenerationName(inventory, year, month)}");

            generator.Generate(inventory, year, month, context);
        }
    }
}
