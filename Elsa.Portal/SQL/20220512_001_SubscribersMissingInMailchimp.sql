IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE Name = N'xrep_SubscribersMissingInMailchimp')
	DROP PROCEDURE xrep_SubscribersMissingInMailchimp;

GO

CREATE PROCEDURE xrep_SubscribersMissingInMailchimp(@projectId INT)
AS
BEGIN
	/*Title:Odběratelé newsletteru čekající na import do MailChimp */

	SELECT cus.Email
    FROM Customer cus
    WHERE cus.ProjectId = @projectId
    AND cus.NewsletterSubscriber = 1
    AND cus.Email NOT IN (SELECT sub.Email 
                            FROM NewsletterSubscriber sub 
						    WHERE sub.ProjectId = @projectId
						    AND sub.SourceName = 'Mailchimp');
END