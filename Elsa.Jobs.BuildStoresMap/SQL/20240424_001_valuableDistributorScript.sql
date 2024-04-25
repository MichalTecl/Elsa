IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'MarkValuableDistributors')
	DROP PROCEDURE MarkValuableDistributors;

GO

CREATE PROCEDURE MarkValuableDistributors
AS
BEGIN
	UPDATE Customer 
	  SET ValuableDistributorFrom = GETDATE()
	WHERE Id In (
		SELECT c.Id
		  FROM Customer c
		  JOIN PurchaseOrder po ON (c.ErpUid = po.CustomerErpUid)
		 WHERE c.IsDistributor = 1  
		   AND c.ValuableDistributorTo is null
		   AND c.ValuableDistributorFrom is null		   
		   AND po.OrderStatusId = 5
		GROUP BY c.Id
		HAVING COUNT(po.Id) >= 2
	);
END

 
