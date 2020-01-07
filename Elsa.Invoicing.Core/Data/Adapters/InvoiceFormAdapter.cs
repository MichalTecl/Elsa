using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common.Data;
using Elsa.Common.SysCounters;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Common.SystemCounters;
using Elsa.Core.Entities.Commerce.Inventory;
using Robowire;

namespace Elsa.Invoicing.Core.Data.Adapters
{
    internal class InvoiceFormAdapter : AdapterBase<IInvoiceForm>, IInvoiceForm
    {
        public InvoiceFormAdapter(IServiceLocator serviceLocator, IInvoiceForm adaptee) : base(serviceLocator, adaptee)
        {
        }

        public int Id => Adaptee.Id;
        public int ProjectId { get => Adaptee.ProjectId; set => Adaptee.ProjectId = value; }
        public string InvoiceNumber { get => Adaptee.InvoiceNumber; set => Adaptee.InvoiceNumber = value; }
        public string InvoiceVarSymbol { get => Adaptee.InvoiceVarSymbol; set => Adaptee.InvoiceVarSymbol = value; }
        public string InvoiceFormNumber { get => Adaptee.InvoiceFormNumber; set => Adaptee.InvoiceFormNumber = value; }
        public int FormTypeId { get => Adaptee.FormTypeId; set => Adaptee.FormTypeId = value; }
        public DateTime IssueDate { get => Adaptee.IssueDate; set => Adaptee.IssueDate = value; }
        public int? CancelUserId { get => Adaptee.CancelUserId; set => Adaptee.CancelUserId = value; }
        public DateTime? CancelDt { get => Adaptee.CancelDt; set => Adaptee.CancelDt = value; }
        public string CancelReason { get => Adaptee.CancelReason; set => Adaptee.CancelReason = value; }
        public int? SupplierId { get => Adaptee.SupplierId; set => Adaptee.SupplierId = value; }
        public int MaterialInventoryId { get => Adaptee.MaterialInventoryId; set => Adaptee.MaterialInventoryId = value; }
        public int InvoiceFormCollectionId { get => Adaptee.InvoiceFormCollectionId; set => Adaptee.InvoiceFormCollectionId = value; }
        public string PriceCalculationLog { get => Adaptee.PriceCalculationLog; set => Adaptee.PriceCalculationLog = value; }
        public bool? PriceHasWarning { get => Adaptee.PriceHasWarning; set => Adaptee.PriceHasWarning = value; }
        public string Text { get => Adaptee.Text; set => Adaptee.Text = value; }
        public string Explanation { get => Adaptee.Explanation; set => Adaptee.Explanation = value; }
        public int? SourceTaskId { get => Adaptee.SourceTaskId; set => Adaptee.SourceTaskId = value; }
        public int? CounterId { get => Adaptee.CounterId; set => Adaptee.CounterId = value; }

        public IInvoiceFormType FormType =>
            Get<IInvoiceFormsRepository, IInvoiceFormType>("FormType", r => r.GetInvoiceFormType(FormTypeId));

        public IUser CancelUser => Get<IUserRepository, IUser>("CancelUser",
            r => CancelUserId == null ? null : r.GetUser(CancelUserId.Value));

        public IReleasingFormsGenerationTask SourceTask => Get<IInvoiceFormsRepository, IReleasingFormsGenerationTask>(
            "SourceTask",
            r => SourceTaskId == null ? null : r.GetReleasingFormsTasks().FirstOrDefault(t => t.Id == SourceTaskId));

        public ISystemCounter Counter => Get<ISysCountersManager, ISystemCounter>("Counter",
            r => CounterId == null ? null : r.GetCounter(CounterId.Value));

        public IMaterialInventory MaterialInventory => Get<IMaterialRepository, IMaterialInventory>("MaterialInventory",
            r => r.GetMaterialInventories().FirstOrDefault(i => i.Id == MaterialInventoryId));

        public string SecondaryInventory
        {
            get => Adaptee.SecondaryInventory;
            set => Adaptee.SecondaryInventory = value;
        }

        public IEnumerable<IInvoiceFormItem> Items =>
            Get<IInvoiceFormsRepository, IEnumerable<IInvoiceFormItem>>("Items", r => r.GetItemsByFormId(Id));

        public ISupplier Supplier => Get<ISupplierRepository, ISupplier>("Supplier",
            r => SupplierId == null ? null : r.GetSupplier(SupplierId.Value));

        public IInvoiceFormCollection InvoiceFormCollection { get; }
        public IProject Project { get; }

    }
}
