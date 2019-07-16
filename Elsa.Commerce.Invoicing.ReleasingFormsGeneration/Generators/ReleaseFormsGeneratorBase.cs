﻿using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Commerce.Core.Model.BatchPriceExpl;
using Elsa.Commerce.Core.Warehouse;
using Elsa.Common;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Invoicing.Core.Contract;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Commerce.Invoicing.ReleasingFormsGeneration.Generators
{
    public abstract class ReleaseFormsGeneratorBase<TItemDescriptor> : IInvoiceFormGenerator
    {
        private readonly IMaterialBatchFacade m_batchFacade;

        protected ReleaseFormsGeneratorBase(IMaterialBatchFacade batchFacade, 
            IInvoiceFormsRepository invoiceFormsRepository)
        {
            m_batchFacade = batchFacade;

            FormType = invoiceFormsRepository.GetInvoiceFormTypes().FirstOrDefault(t =>
                t.GeneratorName.Equals("ReleasingForm", StringComparison.InvariantCultureIgnoreCase));

            if (FormType == null)
            {
                throw new InvalidOperationException("No InvoiceFormType found by 'ReleasingForm'");
            }
        }

        protected IInvoiceFormType FormType { get; }

        protected abstract string FormText { get; }

        public string GetGenerationName(IMaterialInventory forInventory, int year, int month)
        {
            return $"Výdejky ze skladu '{forInventory.Name}' typu '{FormText}' pro {month}/{year}";
        }

        public void Generate(IMaterialInventory forInventory, int year, int month, IInvoiceFormGenerationContext context,
            IReleasingFormsGenerationTask task = null)
        {
            string GetKey(ItemReleaseModel i) => $"{i.Date.Date}:{i.TookFromBatch.MaterialId}";

            var index = new Dictionary<string, List<ItemReleaseModel>>();
            
            GenerateItems(forInventory, year, month, context, task, (time, batch, amount, descriptor) =>
            {
                var item = new ItemReleaseModel(time, batch, amount, descriptor);
                var key = GetKey(item);

                if (!index.TryGetValue(key, out var items))
                {
                    items = new List<ItemReleaseModel>();
                    index.Add(key, items);
                }

                item.Price = GetPrice(item);

                if (item.Price.Item2.HasWarning)
                {
                    context.Warning($"Výpocet ceny šarže \"{item.TookFromBatch.GetTextInfo()}\" není konečný: {item.Price.Item2.Text}");
                }

                items.Add(item);
            });
            
            foreach (var key in index.Keys)
            {
                var list = index[key];

                var totalPriceModel = BatchPrice.Combine(list.Select(i => i.Price.Item2));
                
                var form = context.NewInvoiceForm(f =>
                {
                    f.InvoiceFormNumber = $"NESCHVALENO_{Guid.NewGuid():N}";
                    f.IssueDate = list[0].Date.Date;
                    f.MaterialInventoryId = forInventory.Id;
                    f.FormTypeId = FormType.Id;
                    f.Text = task?.FormText ?? FormText;
                    f.PriceCalculationLog = totalPriceModel.Text;
                    f.PriceHasWarning = totalPriceModel.HasWarning;
                    f.SourceTaskId = task?.Id;

                    CustomizeFormCreation(list, f);
                });
                
                foreach (var item in list)
                {
                    var formItem = context.NewFormItem(form, item.TookFromBatch, i =>
                        {
                            i.MaterialName = item.TookFromBatch.Material.Name;
                            i.Quantity = item.TookAmount.Value;
                            i.UnitId = item.TookAmount.Unit.Id;

                            i.PrimaryCurrencyPrice = item.Price.Item1;

                            CustomizeFormItemCreation(item, i);
                        });

                    OnAfterItemSaved(form, formItem, item);
                }

            }
        }

        protected virtual void CustomizeFormCreation(List<ItemReleaseModel> formItems, IInvoiceForm form) { }

        protected virtual void CustomizeFormItemCreation(ItemReleaseModel releaseModel, IInvoiceFormItem item) { }

        protected virtual void OnAfterItemSaved(IInvoiceForm form, IInvoiceFormItem item, ItemReleaseModel releaseModel) { }

        protected virtual Tuple<decimal, BatchPrice> GetPrice(ItemReleaseModel item)
        {
            return m_batchFacade.GetPriceOfAmount(item.TookFromBatch.Id, item.TookAmount);
        }

        protected abstract void GenerateItems(IMaterialInventory forInventory, int year, int month,
            IInvoiceFormGenerationContext context,
            IReleasingFormsGenerationTask task, Action<DateTime, IMaterialBatch, Amount, TItemDescriptor> itemCallback);
        
        protected sealed class ItemReleaseModel
        {
            public readonly DateTime Date;
            public readonly IMaterialBatch TookFromBatch;
            public readonly Amount TookAmount;
            public readonly TItemDescriptor Descriptor;

            public Tuple<decimal, BatchPrice> Price { get; set; }

            public ItemReleaseModel(DateTime date, IMaterialBatch tookFromBatch, Amount tookAmount, TItemDescriptor descriptor)
            {
                Date = date;
                TookFromBatch = tookFromBatch;
                TookAmount = tookAmount;
                Descriptor = descriptor;
            }
        }
    }


}
