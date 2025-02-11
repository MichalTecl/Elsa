IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'getDistributorsSales')
	DROP PROCEDURE getDistributorsSales;

GO

CREATE PROCEDURE getDistributorsSales (@projectId INT, @historyDepth INT)
AS
BEGIN
	SELECT c.Id CustomerId, DATEDIFF(month, po.BuyDate, GETDATE()) MonthDiff, SUM(po.PriceWithVat) Total
	  FROM Customer c
	  JOIN PurchaseOrder po ON (po.CustomerErpUid = c.ErpUid)
	  WHERE c.ProjectId = @projectId
		ANd po.OrderStatusId = 5
		AND po.BuyDate > DATEADD(month,-1 * @historyDepth, GETDATE())
		AND c.IsCompany = 1
		AND c.IsDistributor = 1
	GROUP BY c.Id, DATEDIFF(month, po.BuyDate, GETDATE())	;
END