using System;
using System.Collections;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IPurchaseOrder
    {
        long Id { get; }
        int ProjectId { get; set; }

        IProject Project { get; }

        int? ErpId { get; set; }

        IErp Erp { get; }

        [NVarchar(64, false)]
        string OrderNumber { get; set; }

        [NVarchar(64, true)]
        string PreInvoiceId { get; set; }

        [NVarchar(64, true)]
        string InvoiceId { get; set; }

        decimal Price { get; set; }
        decimal PriceWithVat { get; set; }
        DateTime PurchaseDate { get; set; }
        DateTime BuyDate { get; set; }
        DateTime DueDate { get; set; }

        [NVarchar(64, false)]
        string VarSymbol { get; set; }

        int CurrencyId { get; set; }

        ICurrency Currency { get; }

        [NVarchar(64, false)]
        string ErpStatusName { get; set; }

        [NVarchar(64, false)]
        string ErpStatusId { get; set; }

        [NVarchar(64, false)]
        string ElsaOrderStatus { get; set; }

        [NVarchar(512, false)]
        string ShippingMethodName { get; set; }

        [NVarchar(512, false)]
        string PaymentMethodName { get; set; }

        [NVarchar(255, false)]
        string CustomerName { get; set; }

        [NVarchar(255, false)]
        string CustomerEmail { get; set; }

        int? InvoiceAddressId { get; set; }
        IAddress InvoiceAddress { get; }
        int? DeliveryAddressId { get; set; }
        IAddress DeliveryAddress { get; }

        [NVarchar(-1, true)]
        string CustomerNote { get; set; }

        [NVarchar(-1, true)]
        string InternalNote { get; set; }

        DateTime? ErpLastChange { get; set; }
        decimal TaxedShippingCost { get; set; }
        decimal TaxedPaymentCost { get; set; }
        decimal ShippingTaxPercent { get; set; }
        decimal PaymentTaxPercent { get; set; }

        IEnumerable<IOrderItem> Items { get; }

        int InsertUserId { get; set; }
        IUser InsertUser { get; }
        DateTime InsertDt { get; set; }
    }
}
