IF OBJECT_ID('dbo.GetCustomerMailConversatons', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetCustomerMailConversatons;
GO

CREATE PROCEDURE dbo.GetCustomerMailConversatons
    @customerId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT mc.Id
      FROM vwAllDistributorEmails ade
      JOIN MessageParticipantAddress mpa ON (mpa.Email = ade.Email)
      JOIN MailMessageReferenceParticipant mmrp ON (mmrp.ParticipantAddressId = mpa.Id)
      JOIN MailMessageReference mmr ON (mmrp.MailMessageReferenceId = mmr.Id)
      JOIN MailConversation mc ON (mmr.ConversationId = mc.Id)
     WHERE ade.CustomerId = @customerId;
END;
GO
