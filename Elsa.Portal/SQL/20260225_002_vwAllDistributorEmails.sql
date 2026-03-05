CREATE OR ALTER VIEW dbo.vwAllDistributorEmails
AS
WITH addresses AS
(
    SELECT c.Id AS CustomerId, c.Email, 'CustomerRecord' LinkType
    FROM Customer c

    UNION

    SELECT ccp.CustomerId, p.Email, 'ContactPerson' LinkType
    FROM CustomerContactPerson ccp
    JOIN Person p ON ccp.PersonId = p.Id

    UNION

    SELECT c.Id AS CustomerId, cec.NewEmail AS Email, 'CustomerEmailChange' LinkType
    FROM CustomerEmailChange cec
    JOIN Customer c ON cec.ErpUid = c.ErpUid

    UNION

    SELECT c.Id AS CustomerId, cec.OldEmail AS Email, 'CustomerEmailChange' LinkType
    FROM CustomerEmailChange cec
    JOIN Customer c ON cec.ErpUid = c.ErpUid
)
SELECT a.CustomerId, a.Email, MIN(a.LinkType) InfoSource
FROM addresses a
JOIN Customer c ON a.CustomerId = c.Id
WHERE a.Email IS NOT NULL
  AND c.IsDistributor = 1
GROUP BY a.CustomerId, a.Email;
GO