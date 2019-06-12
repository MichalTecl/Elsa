using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Apps.InvoiceForms.Model;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Core.CurrencyConversions;
using Elsa.Invoicing.Core.Data;

namespace Elsa.Apps.InvoiceForms.Facade
{
    public class InvoiceFormsQueryingFacade
    {
        private readonly ILog m_log;
        private readonly IInvoiceFormsRepository m_invoiceFormsRepository;
        private readonly ISession m_session;

        public InvoiceFormsQueryingFacade(ILog log, IInvoiceFormsRepository invoiceFormsRepository, ISession session)
        {
            m_log = log;
            m_invoiceFormsRepository = invoiceFormsRepository;
            m_session = session;
        }

        public InvoiceFormsCollection<T> Load<T>(int invoiceFormTypeId, int year, int month,
            Func<IInvoiceForm, T> itemMapper) where T : InvoiceFormModelBase
        {
            var coll = m_invoiceFormsRepository.FindCollection(invoiceFormTypeId, year, month);

            if (coll == null)
            {
                return new InvoiceFormsCollection<T>()
                {
                    CanApprove = false,
                    CanGenerate = true,
                    InvoiceFormTypeId = invoiceFormTypeId,
                    IsGenerated = false,
                    Month = month,
                    Year = year
                };
            }
            
            var homeUrl = m_session.Project.HomeUrl ?? "Project.HomeUrl";

            var collection = new InvoiceFormsCollection<T>();
            foreach (var form in coll.Forms)
            {
                var itemModel = itemMapper(form);
                itemModel.InvoiceFormId = form.Id;
                itemModel.IssueDate = StringUtil.FormatDate(form.IssueDate);
                itemModel.InvoiceFormNumber = form.InvoiceFormNumber;
                itemModel.PrimaryCurrencyPriceWithoutVat = form.Items.Sum(i => i.PrimaryCurrencyPrice);

                itemModel.FormattedPrimaryCurrencyPriceWithoutVat =
                    StringUtil.FormatDecimal(itemModel.PrimaryCurrencyPriceWithoutVat);

                itemModel.CancelReason = form.CancelDt == null ? string.Empty : form.CancelReason ?? "STORNO";
                itemModel.InventoryName = form.MaterialInventory?.Name;
                itemModel.DownloadUrl = $"{StringUtil.JoinUrlSegments(homeUrl, "/invoiceforms/DownloadInvoiceForm")}?id={form.Id}";

                ManageSourceCurrency(itemModel, form);

                collection.Forms.Add(itemModel);
            }
            
            collection.Year = coll.Year;
            collection.Month = coll.Month;
            collection.InvoiceFormTypeId = coll.InvoiceFormTypeId;
            collection.Title = coll.Name ?? "InvoiceFormType.CollectionName not set";
            collection.TotalPriceFormatted =
                StringUtil.FormatDecimal(collection.Forms.Sum(i => i.PrimaryCurrencyPriceWithoutVat));

            collection.CanApprove = (coll.ApproveDt == null) && IsLogClear(coll.Log);
            collection.CanGenerate = (coll.ApproveDt == null);
            collection.IsGenerated = true;
            collection.IsApproved = (coll.ApproveDt != null);
            collection.CanDelete = !collection.IsApproved;

            GroupLogEntries(coll.Log, collection.Log);

            return collection;
        }

        private void GroupLogEntries(IEnumerable<IInvoiceFormGenerationLog> collLog, List<GenerationInfoModel> target)
        {
            foreach (var src in collLog)
            {
                var model = target.FirstOrDefault(t =>
                    (t.Message == src.Message) &&
                    (t.IsError == src.IsError) &&
                    (t.IsWarning == src.IsWarning) &&
                    (t.CanApprove == (src.ApproveDt == null)));
                if (model == null)
                {
                    model = new GenerationInfoModel(src);
                    target.Add(model);
                }

                model.GroupedRecords.Add(src.Id);
            }
        }

        private bool IsLogClear(IEnumerable<IInvoiceFormGenerationLog> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.IsError)
                {
                    return false;
                }

                if (entry.IsWarning && (entry.ApproveDt == null))
                {
                    return false;
                }
            }

            return true;
        }

        private void ManageSourceCurrency<T>(T itemModel, IInvoiceForm form)
            where T : InvoiceFormModelBase
        {
            ICurrency sourceCurrency = null;
            ICurrencyRate rateInfo = null;

            foreach (var item in form.Items)
            {
                if (sourceCurrency == null)
                {
                    sourceCurrency = item.SourceCurrency;
                }

                if ((sourceCurrency?.Id ?? -1) != (item.SourceCurrencyId ?? -1))
                {
                    throw new InvalidOperationException($"Faktura {form.InvoiceNumber} má položky v různých měnách, nelze zpracovat");
                }

                if (rateInfo == null)
                {
                    rateInfo = item.Conversion?.CurrencyRate;
                }

                if ((rateInfo?.Id ?? -1) != (item.Conversion?.CurrencyRate?.Id ?? -1))
                {
                    throw new InvalidOperationException($"Faktura {form.InvoiceNumber} má položky v cizí měně, které byly převedeny za použití různých měnových kurzů, nelze zpracovat");
                }
            }
            
            if (sourceCurrency == null)
            {
                return;
            }

            if (rateInfo == null)
            {
                throw new InvalidOperationException($"Pro fakturu {form.InvoiceNumber} chybi informace o prevodnim kurzu, nelze zpracovat");
            }

            var sum = form.Items.Sum(i => i.SourceCurrencyPrice ?? 0);

            itemModel.OriginalCurrencyPrice = StringUtil.FormatDecimal(sum);
            itemModel.ConversionRate = StringUtil.FormatDecimal(rateInfo.Rate);
            itemModel.ConversionRateLink = rateInfo.SourceLink;
            itemModel.OriginalCurrencySymbol = sourceCurrency.Symbol;
        }
    }
}
