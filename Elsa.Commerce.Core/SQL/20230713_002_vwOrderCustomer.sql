IF OBJECT_ID('vwOrderCustomer', 'V') IS NOT NULL
    DROP VIEW vwOrderCustomer
GO

CREATE VIEW vwOrderCustomer AS
SELECT po.Id AS OrderId, ISNULL(c1.cid, c2.cid) AS CustomerId,
    CASE 
        WHEN c1.cid IS NULL THEN 'Email' 
        ELSE 'UID' 
    END AS CustomerSeekMethod
FROM PurchaseOrder po
LEFT JOIN (
    SELECT c.ErpUid, MAX(c.Id) AS cid
    FROM Customer c
    GROUP BY c.ErpUid
) c1 ON c1.ErpUid = po.CustomerErpUid
LEFT JOIN (
    SELECT c.Email, MAX(c.Id) AS cid
    FROM Customer c
    GROUP BY c.Email
) c2 ON c2.Email = po.CustomerEmail
GO