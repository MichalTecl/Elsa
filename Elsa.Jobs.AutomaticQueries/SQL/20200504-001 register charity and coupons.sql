IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'autoq_countCharitySales')
BEGIN
	DROP PROCEDURE autoq_countCharitySales;
END

GO

CREATE PROCEDURE autoq_countCharitySales (@projectId INT, @year INT, @month INT)
AS
BEGIN
	SELECT sum(oi.Quantity) AS [Prodano nahatych]
		FROM PurchaseOrder po
		JOIN OrderItem     oi ON (oi.PurchaseOrderId = po.Id)
		WHERE YEAR(po.PackingDt) = @year
         AND MONTH(po.PackingDt) = @month
	     AND po.OrderStatusId = 5
	     AND oi.PlacedName like N'%náhradní%';
END

GO

IF NOT EXISTS(SELECT * FROM AutomaticQuery WHERE ProcedureName = 'autoq_countCharitySales')
BEGIN
	INSERT INTO AutomaticQuery (TitlePattern, ProcedureName, MailRecipientGroup, ResultToAttachment, ProjectId)
	VALUES (N'Nahaté deoše @month/@year', 'autoq_countCharitySales', 'Autom. dotazy', 1, 1);

	DECLARE @queryId INT = SCOPE_IDENTITY();

	INSERT INTO AutoQueryParameter (QueryId, ParameterName, Expression, TriggerOnly)
	SELECT @queryId, '@projectId', 'GET_PROJECT_ID', 0 UNION
    SELECT @queryId, '@year', 'GET_PREV_MONTH_YEAR', 0 UNION
	SELECT @queryId, '@month', 'GET_PREV_MONTH', 0;
END

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE name = 'autoq_discountCoupons')
BEGIN
	DROP PROCEDURE autoq_discountCoupons;
END

GO

CREATE PROCEDURE autoq_discountCoupons (@projectId INT, @year INT, @month INT)
AS
BEGIN
	select po.DiscountsText [Typ slevy], 
		   po.PackingDt [Datum], 
		   po.CustomerName [Zakaznik], 
		   po.InvoiceId [Faktura], 
		   po.VarSymbol [VS], 
		   po.Price [Cena bez DPH]
		  from PurchaseOrder po
	where year(po.packingdt) = @year
	  and month(po.packingdt) = @month
	  and po.DiscountsText like N'Poukaz s 20[%] slevou:%'
	  and po.ProjectId = @projectId
	ORDER BY po.DiscountsText, po.PackingDt
END

GO

IF NOT EXISTS(SELECT * FROM AutomaticQuery WHERE ProcedureName = 'autoq_discountCoupons')
BEGIN
	INSERT INTO AutomaticQuery (TitlePattern, ProcedureName, MailRecipientGroup, ResultToAttachment, ProjectId)
	VALUES (N'Slevové poukazy @month/@year', 'autoq_discountCoupons', 'Autom. dotazy', 1, 1);

	DECLARE @queryId INT = SCOPE_IDENTITY();

	INSERT INTO AutoQueryParameter (QueryId, ParameterName, Expression, TriggerOnly)
	SELECT @queryId, '@projectId', 'GET_PROJECT_ID', 0 UNION
    SELECT @queryId, '@year', 'GET_PREV_MONTH_YEAR', 0 UNION
	SELECT @queryId, '@month', 'GET_PREV_MONTH', 0;
END