IF OBJECT_ID('dbo.GetCustomerMailConversations', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetCustomerMailConversations;
GO

CREATE PROCEDURE dbo.GetCustomerMailConversations
    @customerId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT mc.Id,
           mc.ConversationEndDt,
           ISNULL(mcs.SubjectSummary, mc.Hint) [Subject],
           mcs.Summary,
           COUNT(DISTINCT mmr.Id) MessageCount
      FROM (
            SELECT DISTINCT ade.Email
              FROM vwAllDistributorEmails ade
             WHERE ade.CustomerId = @customerId
           ) customerEmails
      JOIN MessageParticipantAddress mpa ON (mpa.Email = customerEmails.Email)
      JOIN MailMessageReferenceParticipant mmrp ON (mmrp.ParticipantAddressId = mpa.Id)
      JOIN MailMessageReference mmr ON (mmrp.MailMessageReferenceId = mmr.Id)
      JOIN MailConversation mc ON (mmr.ConversationId = mc.Id)
      JOIN MailConversationSummary mcs ON (mc.SummaryId = mcs.Id)
     GROUP BY mc.Id, mc.ConversationEndDt, ISNULL(mcs.SubjectSummary, mc.Hint), mcs.Summary
    HAVING COUNT(DISTINCT mmr.Id) > 1;
END;
GO
