using System.Collections.Generic;

namespace Elsa.Commerce.Core.Model
{
    public interface IErpOrderModel
    {
        int ErpSystemId { get; }

        string ErpOrderId { get; }
        string OrderNumber { get; }
        string DueDate { get; }
        string PreInvId { get; }

        string Price { get; }

        string PriceWithVat { get; }
        string PurchaseDate { get; }

        string BuyDate { get; }

        string VarSymb { get; }

        #region Invoice Address
        string InvoiceCompanyName { get; }
        string InvoiceFirstName { get; }
        string InvoiceSurname { get; }
        string InvoiceStreet { get; }
        string InvoiceDescriptiveNumber { get; }
        string InvoiceOrientationNumber { get; }
        string InvoiceCity { get; }
        string InvoiceZip { get; }
        string InvoiceCountry { get; }
        string InvoicePhone { get; }
        #endregion

        #region Delivery Address
        string DeliveryCompanyName { get; }
        string DeliveryName { get; }
        string DeliverySurname { get; }
        string DeliveryStreet { get; }
        string DeliveryDescriptiveNumber { get; }
        string DeliveryOrientationNumber { get; }
        string DeliveryCity { get; }
        string DeliveryZip { get; }
        string DeliveryCountry { get; }
        string DeliveryPhone { get; }
        #endregion

        string CurrencyCode { get; }

        string ErpStatusName { get; }

        string ErpShippingName { get; }

        string ErpPaymentName { get; }

        string Customer { get; }

        string Email { get; }

        string UserId { get; }

        string PayDate { get; }

        string Paid { get; }

        string ErpStatus { get; }

        string NoteLeftByCustomer { get; }

        string InternalNote { get; }

        string InvoiceSent { get; }

        string InvoiceId { get; }

        string PreviewText { get; }

        string DeliveryFormattedHouseNumber { get; }

        string FormattedHouseNumber { get; }

        bool IsPayOnDelivery { get; }
        

        IEnumerable<IErpPriceElementModel> OrderPriceElements { get; }

        IEnumerable<IErpOrderItemModel> LineItems { get; }

    }
}
