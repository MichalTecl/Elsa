using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Common
{
    [Entity]
    public interface IPayment
    {
        long Id { get; }

        int ProjectId { get; set; }

        int PaymentSourceId { get; set; }

        IPaymentSource PaymentSource { get; }

        IProject Project { get; }

        [NotFk]
        [NVarchar(256, false)]
        string TransactionId { get; set; }

        DateTime PaymentDt { get; set; }

        decimal Value { get; set; }

        int CurrencyId { get; set; }

        ICurrency Currency { get; }
        
        [NVarchar(255, false)]
        string RemoteAccountNumber { get; set; }

        [NVarchar(64, false)]
        string RemoteAccountBankCode { get; set; }

        [NVarchar(255, false)]
        string RemoteBankName { get; set; }

        [NVarchar(255, false)]
        string ConstantSymbol { get; set; }

        [NVarchar(255, false)]
        string SpecificSymbol { get; set; }

        [NVarchar(255, false)]
        string VariableSymbol { get; set; }

        [NVarchar(255, false)]
        string Message { get; set; }

        [NVarchar(255, false)]
        string SearchText { get; set; }

        [NVarchar(100, true)]
        string SenderName { get; set; }

        [ForeignKey(nameof(IPurchaseOrder.PaymentId))]
        IEnumerable<IPurchaseOrder> Orders { get; }
    }
}
