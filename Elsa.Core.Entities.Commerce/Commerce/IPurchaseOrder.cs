using System;
using System.Collections.Generic;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Core.Entities.Commerce.Core;
using Elsa.Core.Entities.Commerce.Integration;

using Robowire.RobOrm.Core;
using Robowire.RobOrm.SqlServer.Attributes;

namespace Elsa.Core.Entities.Commerce.Commerce
{
    [Entity]
    public interface IPurchaseOrder : IProjectRelatedEntity
    {
        long Id { get; }

        int? ErpId { get; set; }

        IErp Erp { get; }

        [NVarchar(64, false)]
        string OrderNumber { get; set; }

        [NotFk]
        [NVarchar(64, true)]
        string PreInvoiceId { get; set; }

        [NotFk]
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

        [NotFk]
        [NVarchar(64, false)]
        string ErpStatusId { get; set; }
        
        int OrderStatusId { get; set; }

        IOrderStatus OrderStatus { get; }

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

        IEnumerable<IOrderPriceElement> PriceElements { get; }

        int InsertUserId { get; set; }
        IUser InsertUser { get; }
        DateTime InsertDt { get; set; }

        [NVarchar(32, false)]
        string OrderHash { get; set; }

        bool IsPayOnDelivery { get; set; }

        long? PaymentId { get; set; }

        IPayment Payment { get; }

        int? PaymentPairingUserId { get; set; }

        IUser PaymentPairingUser { get; }

        DateTime? PaymentPairingDt { get; set; }

        [NotFk]
        [NVarchar(255, false)]
        string ErpOrderId { get; set; }

        int? PackingUserId { get; set; }
        IUser PackingUser { get; }

        DateTime? PackingDt { get; set; }

        [NVarchar(1000, true)]
        string DiscountsText { get; set; }

        DateTime? ReturnDt { get; set; }

        [NVarchar(100, true)]
        string CustomerErpUid { get; set; }
        
        [NVarchar(255, true)]
        string PercentDiscountText { get; set; }

        decimal? PercentDiscountValue { get; set; }
    }
}
