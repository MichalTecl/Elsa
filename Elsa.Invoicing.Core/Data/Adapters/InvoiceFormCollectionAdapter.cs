using System;
using System.Collections.Generic;
using Elsa.Commerce.Core;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire;

namespace Elsa.Invoicing.Core.Data.Adapters
{
    internal class InvoiceFormCollectionAdapter : AdapterBase<IInvoiceFormCollection>, IInvoiceFormCollection
    {
        public InvoiceFormCollectionAdapter(IServiceLocator serviceLocator, IInvoiceFormCollection adaptee) : base(serviceLocator, adaptee)
        {
        }

        public int Id => Adaptee.Id;
        public int ProjectId { get; set; }
        public IProject Project { get; }
        public string Name { get => Adaptee.Name; set => Adaptee.Name = value; }
        public int GenerateUserId { get => Adaptee.GenerateUserId; set => Adaptee.GenerateUserId = value; }
        public IUser GenerateUser => Get<IUserRepository, IUser>("GenerateUser", r => r.GetUser(GenerateUserId));
        public int? ApproveUserId { get => Adaptee.ApproveUserId; set => Adaptee.ApproveUserId = value; }

        public IUser ApproveUser => Get<IUserRepository, IUser>("ApproveUser",
            r => ApproveUserId == null ? null : r.GetUser(ApproveUserId.Value));
        public DateTime? ApproveDt { get => Adaptee.ApproveDt; set => Adaptee.ApproveDt = value; }

        public IEnumerable<IInvoiceForm> Forms =>
            Get<IInvoiceFormsRepository, IEnumerable<IInvoiceForm>>("Forms", r => r.GetInvoiceFormsByCollectionId(Id));

        public IEnumerable<IInvoiceFormGenerationLog> Log =>
            Get<IInvoiceFormsRepository, IEnumerable<IInvoiceFormGenerationLog>>("Log",
                r => r.GetLogByCollectionId(Id));

        public int InvoiceFormTypeId { get => Adaptee.InvoiceFormTypeId; set => Adaptee.InvoiceFormTypeId = value; }

        public IInvoiceFormType InvoiceFormType =>
            Get<IInvoiceFormsRepository, IInvoiceFormType>("InvoiceFormType",
                r => r.GetInvoiceFormType(InvoiceFormTypeId));

        public int Year { get => Adaptee.Year; set => Adaptee.Year = value; }
        public int Month { get => Adaptee.Month; set => Adaptee.Month = value; }
    }
}
