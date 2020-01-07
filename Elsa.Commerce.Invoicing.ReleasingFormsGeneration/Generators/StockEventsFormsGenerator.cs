using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elsa.Commerce.Core.StockEvents;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Accounting.InvoiceFormItemBridges;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;
using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Invoicing.ReleasingFormsGeneration.Generators
{
    public class StockEventsFormsGenerator : ReleaseFormsGeneratorBase<StockEventDescriptor>
    {
        private readonly IStockEventRepository m_stockEventRepository;
        private readonly IDatabase m_database;

        public StockEventsFormsGenerator(IMaterialBatchFacade batchFacade,
            IInvoiceFormsRepository invoiceFormsRepository, IMaterialRepository materialRepository,
            IStockEventRepository stockEventRepository, IDatabase database) : base(batchFacade, invoiceFormsRepository,
            materialRepository)
        {
            m_stockEventRepository = stockEventRepository;
            m_database = database;
        }

        protected override string GetExplanation(List<ItemReleaseModel> item, IInvoiceForm invoiceForm)
        {
            var notes = string.Join("; ",
                item.Where(it => !string.IsNullOrWhiteSpace(it.Descriptor.StockEventNote))
                    .Select(it => it.Descriptor.StockEventNote).Distinct());

            if (!string.IsNullOrWhiteSpace(notes))
            {
                notes = $" ({notes})";
            }

            return $"{item[0].Descriptor.StockEventTypeName} ze šarže {item[0].Descriptor.Event.Batch.GetUnid()}{notes}";
        }

        protected override void GenerateItems(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context,
            IReleasingFormsGenerationTask task, Action<DateTime, IMaterialBatch, Amount, StockEventDescriptor> itemCallback)
        {
            DateUtil.GetMonthDt(year, month, out var dtFrom, out var dtTo);

            var events = m_stockEventRepository.GetEvents(dtFrom, dtTo, forInventory.Id).Where(e => e.Type.IsSubtracting).ToList();

            foreach(var evt in events)
            {
                if (evt.Batch.IsHiddenForAccounting == true)
                {
                    continue;
                }

                itemCallback(evt.EventDt.Date, evt.Batch, new Amount(evt.Delta, evt.Unit), new StockEventDescriptor()
                {
                    StockEventId = evt.Id,
                    StockEventTypeName = evt.Type.Name,
                    Event = evt,
                    StockEventNote = evt.Note
                });
            }
        }

        protected override void CustomizeFormItemCreation(ItemReleaseModel releaseModel, IInvoiceFormItem item)
        {
            if (!string.IsNullOrWhiteSpace(releaseModel.Descriptor.Event.Note))
            {
                item.Note = releaseModel.Descriptor.Event.Note;
            }
        }

        protected override void CustomizeFormCreation(List<ItemReleaseModel> formItems, IInvoiceForm form)
        {
            if (formItems[0].Descriptor.Event.Type.InvoiceFormNumberCounterId == null)
            {
                throw new InvalidOperationException($"No InvoiceFormNumberCounterId defined for StockEventType = {formItems[0].Descriptor.Event.Type.Name}");
            }

            form.Text = formItems[0].Descriptor.StockEventTypeName;
            form.CounterId = formItems[0].Descriptor.Event.Type.InvoiceFormNumberCounterId;
        }

        protected override string GetGroupingKey(ItemReleaseModel item)
        {
            return $"{item.Date}:{item.Descriptor.StockEventTypeName}:{item.Descriptor.Event.Batch.Id}";
        }

        protected override void OnAfterItemSaved(IInvoiceForm form, IInvoiceFormItem item, ItemReleaseModel releaseModel)
        {
            m_database.Save(m_database.New<IStockEventInvoiceFormItem>(b =>
                {
                    b.MaterialStockEventId = releaseModel.Descriptor.StockEventId;
                    b.InvoiceFormItemId = item.Id;
                }));
        }
    }

    public class StockEventDescriptor
    {
        public int StockEventId { get; set; }

        public string StockEventTypeName { get; set; }

        public IMaterialStockEvent Event { get; set; }

        public string StockEventNote { get; set; }
    }
}
