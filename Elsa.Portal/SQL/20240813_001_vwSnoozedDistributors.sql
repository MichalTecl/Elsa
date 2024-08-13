IF EXISTS(SELECT TOP 1 1 FROM sys.views WHERe name = 'vwSnoozedDistributors')
	DROP VIEW vwSnoozedDistributors;

GO

CREATE VIEW vwSnoozedDistributors
AS
SELECT snooze.CustomerId
FROM (SELECT ds.CustomerId, MAX(ds.SetDt) SetDt
        FROM DistributorSnooze ds
		GROUP BY ds.customerId) snooze 
	JOIN vwCustomerLatestOrder clo ON (clo.CustomerId = snooze.CustomerId)
WHERE DATEDIFF(DAY, snooze.SetDt, clo.LatestSuccessOrderDt) < 0
