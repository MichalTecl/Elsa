IF OBJECT_ID('dbo.GetCustomerMailConversations', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetCustomerMailConversations;
GO

CREATE PROCEDURE dbo.GetCustomerMailConversations
    @customerId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT mc.Id,
           mc.ConversationEndDt,
           ISNULL(mcs.SubjectSummary, mc.Hint) [Subject],
           mcs.Summary
      FROM (
            SELECT DISTINCT ade.Email
              FROM vwAllDistributorEmails ade
             WHERE ade.CustomerId = @customerId
           ) customerEmails
      JOIN MessageParticipantAddress mpa ON (mpa.Email = customerEmails.Email)
      JOIN MailMessageReferenceParticipant mmrp ON (mmrp.ParticipantAddressId = mpa.Id)
      JOIN MailMessageReference mmr ON (mmrp.MailMessageReferenceId = mmr.Id)
      JOIN MailConversation mc ON (mmr.ConversationId = mc.Id)
      JOIN MailConversationSummary mcs ON (mc.SummaryId = mcs.Id);
END;
GO
