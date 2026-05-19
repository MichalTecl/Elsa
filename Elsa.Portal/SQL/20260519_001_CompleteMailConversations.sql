CREATE OR ALTER PROCEDURE CompleteMailConversations
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CreatedCount int = 0;
    DECLARE @AssignedCount int = 0;

    BEGIN TRY
        BEGIN TRAN;

        INSERT INTO dbo.MailConversation (ConversationUid, Hint, ConversationEndDt)
        SELECT fc.ConversationUid,
               MIN(fc.Subject) AS Hint,
               MAX(mr.InternalDt) AS ConversationEndDt
          FROM dbo.MailMessageReference mr
          JOIN dbo.MailMessageFullContent fc
            ON fc.Id = mr.FullContentId
          LEFT JOIN dbo.MailConversation mc
            ON mc.ConversationUid = fc.ConversationUid
         WHERE mr.ConversationId IS NULL
           AND mr.FullContentId IS NOT NULL
           AND mr.ExclusionRule IS NULL
           AND fc.ConversationUid IS NOT NULL
           AND mc.Id IS NULL
         GROUP BY fc.ConversationUid;

        SET @CreatedCount = @@ROWCOUNT;

        UPDATE mr
           SET mr.ConversationId = mc.Id
          FROM dbo.MailMessageFullContent fc
          JOIN dbo.MailMessageReference mr
            ON mr.FullContentId = fc.Id
          JOIN dbo.MailConversation mc
            ON mc.ConversationUid = fc.ConversationUid
         WHERE mr.ConversationId IS NULL
           AND mr.FullContentId IS NOT NULL
           AND mr.ExclusionRule IS NULL
           AND fc.ConversationUid IS NOT NULL;

        SET @AssignedCount = @@ROWCOUNT;

        COMMIT;

        SELECT @CreatedCount AS CreatedConversations,
               @AssignedCount AS AssignedMessages;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
