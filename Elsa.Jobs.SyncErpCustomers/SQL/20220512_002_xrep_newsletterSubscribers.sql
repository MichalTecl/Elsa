IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE Name = 'xrep_newsletterSubscribers')
	DROP PROCEDURE xrep_newsletterSubscribers;

GO

CREATE PROCEDURE [xrep_newsletterSubscribers] (@projectId INT)
AS
BEGIN
 /*title: Odběratelé newsletteru - FB audience */

select c.Email email,       
       Replace(c.Phone, '+', '00') phone,
	   Replace(a.Phone, '+', '00') phone,
	   ISNULL(SUBSTRING(TRIM(c.name), 0, CHARINDEX(' ', TRIM(c.name))), a.FirstName) fn,
	   ISNULL(REVERSE(SUBSTRING(REVERSE(TRIM(c.name)), 0, CHARINDEX(' ', REVERSE(TRIM(c.name))))), a.LastName) ln,
	   a.City ct,
	   a.Zip zip	   
from NewsletterSubscriber sub
left join customer c ON (c.Email = sub.Email)
left join (SELECT o.CustomerEmail, MAX(o.InvoiceAddressId) addrid FROM PurchaseOrder o GROUP BY o.CustomerEmail) ordr on (c.Email = ordr.CustomerEmail)
left join [Address] a on (ordr.addrid = a.Id)
where sub.ProjectId = @projectId 
  AND sub.UnsubscribeDt IS NULL
  AND sub.SourceName = N'Mailchimp'
  and c.ProjectId = @projectId

END


