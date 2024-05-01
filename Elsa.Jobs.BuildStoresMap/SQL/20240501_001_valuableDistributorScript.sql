IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'MarkValuableDistributors')
	DROP PROCEDURE MarkValuableDistributors;

GO

CREATE PROCEDURE MarkValuableDistributors (@projectId INT, @minOrdersCount INT, @maxMonthsFromLastOrder INT)
AS
BEGIN
	DECLARE @valuables TABLE (id int);
	INSERT INTO @valuables
	SELECT c.Id
	--SELECT c.Id, c.Name, ocnt.OrdersCount, DATEADD(month, @maxMonthsFromLastOrder, lor.LatestSuccessOrderDt), lor.LatestSuccessOrderDt
	  FROM Customer c
	  JOIN (SELECT po.CustomerErpUid, COUNT(po.Id) OrdersCount 
			  FROM PurchaseOrder po
			 WHERE po.OrderStatusId = 5
			   AND po.ProjectId = @projectId
			 GROUP BY po.CustomerErpUid) ocnt ON (ocnt.CustomerErpUid = c.ErpUid)
	  JOIN vwCustomerLatestOrder lor ON (lor.CustomerId = c.Id)
	 WHERE c.ProjectId = @projectId
	   AND c.IsDistributor = 1
	   AND c.IsCompany = 1
	   AND ocnt.OrdersCount >= @minOrdersCount
	   AND DATEADD(month, @maxMonthsFromLastOrder, lor.LatestSuccessOrderDt) >= GETDATE();

	UPDATE Customer
	   SET IsValuableDistributor = 1,
		   IsValuableDistributorChangeDt = GETDATE()
	 WHERE ISNULL(IsValuableDistributor, 0) = 0
	   AND Id IN (SELECT id FROM @valuables);

	UPDATE Customer
	   SET IsValuableDistributor = 0,
		   IsValuableDistributorChangeDt = GETDATE()
	 WHERE ISNULL(IsValuableDistributor, 0) = 1
	   AND Id NOT IN (SELECT id FROM @valuables);
END

 
