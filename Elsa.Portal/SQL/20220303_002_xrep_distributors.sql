IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'xrep_distributors')
	DROP PROCEDURE xrep_distributors;

GO

CREATE PROCEDURE xrep_distributors (@projectId INT)
AS
BEGIN
	/*Title: Velkoodběratelé - tržby za poslední rok */
	SELECT ISNULL(c.Name, c.Email) as Odběratel, c.Email as Email, CAST(ROUND(SUM(po.Price), 0) AS INT) as "Celkem nákupy za poslední rok"
	  FROM PurchaseOrder po
	  JOIN (SELECT xcu.Email, MAX(xcu.Id) CustomerId
			  FROM Customer xcu
			 WHERE xcu.IsDistributor = 1
			GROUP BY xcu.Email) lcr ON (po.CustomerEmail = lcr.Email)
	  JOIN Customer c ON (lcr.CustomerId = c.Id)
	WHERE po.OrderStatusId IN (4, 5) -- Packed, Sent
	  AND po.PurchaseDate >= (GETDATE() - 365)
	  AND po.ProjectId = @projectId
	GROUP BY c.Name, c.Email
	ORDER BY SUM(po.Price) DESC;
END


