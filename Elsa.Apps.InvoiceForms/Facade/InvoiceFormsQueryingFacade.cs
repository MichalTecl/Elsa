using System;
using System.Linq;

using Elsa.Apps.InvoiceForms.Model;
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

        public InvoiceFormsQueryingFacade(ILog log, IInvoiceFormsRepository invoiceFormsRepository)
        {
            m_log = log;
            m_invoiceFormsRepository = invoiceFormsRepository;
        }

        public InvoiceFormsCollection<T> Load<T>(int invoiceFormType,
            int? year,
            int? month,
            Func<IInvoiceForm, T> itemMapper) where T : InvoiceFormModelBase
        {
            var type = m_invoiceFormsRepository.GetInvoiceFormTypes().FirstOrDefault(t => t.Id == invoiceFormType);
            if (type == null)
            {
                throw new InvalidOperationException("Invalid invoiceFormTypeId");
            }

            var fromDt = new DateTime(year ?? 2000, month ?? 1, 1);
            var toDt = fromDt.AddMonths(1);

            if (year == null)
            {
                toDt = new DateTime(DateTime.Now.Year, fromDt.Month, 1).AddMonths(1);
            }

            if (month == null)
            {
                toDt = new DateTime(toDt.Year, 11, 1).AddMonths(1);
            }

            m_log.Info($"Invoice forms will be loaded. Type={invoiceFormType}, From={fromDt}, To={toDt}");

            var forms = m_invoiceFormsRepository.FindInvoiceForms(invoiceFormType, null, null, null, fromDt, toDt)
                .OrderBy(f => f.IssueDate)
                .ToList();

            m_log.Info($"Loded {forms.Count} of forms");

            var collection = new InvoiceFormsCollection<T>();
            foreach (var form in forms)
            {
                var itemModel = itemMapper(form);
                itemModel.InvoiceFormId = form.Id;
                itemModel.IssueDate = StringUtil.FormatDate(form.IssueDate);
                itemModel.InvoiceFormNumber = form.InvoiceFormNumber;
                itemModel.PrimaryCurrencyPriceWithoutVat = form.Items.Sum(i => i.PrimaryCurrencyPrice);

                itemModel.FormattedPrimaryCurrencyPriceWithoutVat =
                    StringUtil.FormatDecimal(itemModel.PrimaryCurrencyPriceWithoutVat);

                itemModel.IsCanceled = form.CancelDt != null;

                ManageSourceCurrency(itemModel, form);

                collection.Forms.Add(itemModel);
            }
            
            collection.Year = year;
            collection.Month = month;
            collection.InvoiceFormTypeId = invoiceFormType;
            collection.Title = type.CollectionName ?? "InvoiceFormType.CollectionName not set";
            collection.TotalPriceFormatted =
                StringUtil.FormatDecimal(collection.Forms.Sum(i => i.PrimaryCurrencyPriceWithoutVat));

            return collection;
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
                throw new InvalidOperationException($"Pro fakturu {form.InvoiceNumber} chybi informace o prevodim kurzu, nelze zpracovat");
            }

            var sum = form.Items.Sum(i => i.SourceCurrencyPrice ?? 0);

            itemModel.OriginalCurrencyPrice = StringUtil.FormatDecimal(sum);
            itemModel.ConversionRate = StringUtil.FormatDecimal(rateInfo.Rate);
            itemModel.ConversionRateLink = rateInfo.SourceLink;
            itemModel.OriginalCurrencySymbol = sourceCurrency.Symbol;
        }
    }
}
