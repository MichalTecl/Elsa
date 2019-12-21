IF NOT EXISTS(SELECT TOP 1 1 FROM sys.tables WHERE name = 'PurchaseOrderHistory')
BEGIN
	CREATE TABLE [dbo].[PurchaseOrderHistory](
		[Id] [bigint],
		[ProjectId] [int] NULL,
		[ErpId] [int] NULL,
		[OrderNumber] [nvarchar](64) NULL,
		[PreInvoiceId] [nvarchar](64) NULL,
		[InvoiceId] [nvarchar](64) NULL,
		[VarSymbol] [nvarchar](64) NULL,
		[CurrencyId] [int] NULL,
		[ErpStatusName] [nvarchar](64) NULL,
		[ErpStatusId] [nvarchar](64) NULL,
		[ShippingMethodName] [nvarchar](512) NULL,
		[PaymentMethodName] [nvarchar](512) NULL,
		[CustomerName] [nvarchar](255) NULL,
		[CustomerEmail] [nvarchar](255) NULL,
		[InvoiceAddressId] [int] NULL,
		[DeliveryAddressId] [int] NULL,
		[CustomerNote] [nvarchar](max) NULL,
		[InternalNote] [nvarchar](max) NULL,
		[Price] [decimal](19, 4) NULL,
		[PriceWithVat] [decimal](19, 4) NULL,
		[PurchaseDate] [datetime] NULL,
		[BuyDate] [datetime] NULL,
		[DueDate] [datetime] NULL,
		[ErpLastChange] [datetime] NULL,
		[TaxedShippingCost] [decimal](19, 4) NULL,
		[TaxedPaymentCost] [decimal](19, 4) NULL,
		[ShippingTaxPercent] [decimal](19, 4) NULL,
		[PaymentTaxPercent] [decimal](19, 4) NULL,
		[InsertUserId] [int] NULL,
		[InsertDt] [datetime] NULL,
		[OrderHash] [nvarchar](32) NULL,
		[OrderStatusId] [int] NULL,
		[IsPayOnDelivery] [bit] NULL,
		[PaymentId] [bigint] NULL,
		[PaymentPairingUserId] [int] NULL,
		[PaymentPairingDt] [datetime] NULL,
		[ErpOrderId] [nvarchar](255) NULL,
		[PackingUserId] [int] NULL,
		[PackingDt] [datetime] NULL,
		[DiscountsText] [nvarchar](1000) NULL,
		[ReturnDt] [datetime] NULL,
		[AuditDate] [datetime] NULL);
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.triggers WHERE name = 'TRG_OrdersAudit')
BEGIN
	DROP TRIGGER TRG_OrdersAudit;
END

GO

CREATE TRIGGER TRG_OrdersAudit ON PurchaseOrder
AFTER Update, Insert
AS
BEGIN
	INSERT INTO PurchaseOrderHistory
	SELECT po.*, GETDATE()
	 FROM Inserted po;	 
END

GO

IF NOT EXISTS(SELECT TOP 1 1 FROM PurchaseOrderHistory)
BEGIN
	INSERT INTO PurchaseOrderHistory
	SELECT *, GETDATE() FROM PurchaseOrder 
	WHERE PurchaseDate > DATEADD(month, -3, GETDATE())
END

GO
