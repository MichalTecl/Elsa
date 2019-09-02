using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Accounting;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire;

namespace Elsa.Invoicing.Core.Data.Adapters
{
    internal class InvoiceFormGenerationLogAdapter : AdapterBase<IInvoiceFormGenerationLog>, IInvoiceFormGenerationLog
    {
        public InvoiceFormGenerationLogAdapter(IServiceLocator serviceLocator, IInvoiceFormGenerationLog adaptee) : base(serviceLocator, adaptee)
        {
        }

        public int Id => Adaptee.Id;
        public DateTime EventDt { get => Adaptee.EventDt; set => Adaptee.EventDt = value; }
        public int InvoiceFormCollectionId { get => Adaptee.InvoiceFormCollectionId; set => Adaptee.InvoiceFormCollectionId = value; }
        
        public bool IsError { get => Adaptee.IsError; set=>Adaptee.IsError = value; }
        public bool IsWarning { get => Adaptee.IsWarning; set=>Adaptee.IsWarning = value; }
        public string Message { get => Adaptee.Message; set=>Adaptee.Message=value; }
        public int? ApproveUserId { get => Adaptee.ApproveUserId; set => Adaptee.ApproveUserId = value; }
        public DateTime? ApproveDt { get => Adaptee.ApproveDt; set => Adaptee.ApproveDt = value; }

        public IUser ApproveUser => Get<IUserRepository, IUser>("ApproveUser",
            r => ApproveUserId == null ? null : r.GetUser(ApproveUserId.Value));
        public IInvoiceFormCollection InvoiceFormCollection { get { throw new NotSupportedException("TODO"); } }
    }
}
