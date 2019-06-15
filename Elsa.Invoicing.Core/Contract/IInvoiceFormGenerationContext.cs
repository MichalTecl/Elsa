using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Inventory.Batches;

namespace Elsa.Invoicing.Core.Contract
{
    public interface IInvoiceFormGenerationContext
    {
        bool HasErrors { get; }

        void Info(string inf,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0);

        void Warning(string inf,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0);

        void Error(string message,
            Exception ex = null,
            [CallerMemberName] string member = "",
            [CallerFilePath] string path = "",
            [CallerLineNumber] int line = 0);

        IInvoiceForm NewInvoiceForm(Action<IInvoiceForm> setup);

        IInvoiceFormItem NewFormItem(IInvoiceForm form, IMaterialBatch batch, Action<IInvoiceFormItem> setup);

        void AutoApproveWarnings(HashSet<string> preapprovedMessages);

        int CountForms();
    }
}
