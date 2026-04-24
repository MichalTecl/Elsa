CREATE OR ALTER PROCEDURE CompleteMailConversations
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @CreatedCount int = 0;
    DECLARE @AssignedCount int = 0;

    BEGIN TRY
        BEGIN TRAN;

        IF OBJECT_ID('tempdb..#ConversationSeeds') IS NOT NULL
            DROP TABLE #ConversationSeeds;

        CREATE TABLE #ConversationSeeds
        (
            ConversationUid nvarchar(1000) COLLATE DATABASE_DEFAULT NOT NULL PRIMARY KEY,
            Hint nvarchar(100) COLLATE DATABASE_DEFAULT NULL,
            ConversationEndDt datetime NOT NULL
        );

        INSERT INTO #ConversationSeeds (ConversationUid, Hint, ConversationEndDt)
        SELECT pending.ConversationUid,
               LEFT(COALESCE(latest.Subject, pending.ConversationUid), 100) AS Hint,
               pending.ConversationEndDt
          FROM
          (
              SELECT fc.ConversationUid,
                     MAX(mr.InternalDt) ConversationEndDt
                FROM dbo.MailMessageReference mr
                JOIN dbo.MailMessageFullContent fc
                  ON fc.Id = mr.FullContentId
               WHERE mr.ConversationId IS NULL
                 AND mr.FullContentId IS NOT NULL
                 AND mr.ExclusionRule IS NULL
                 AND fc.ConversationUid IS NOT NULL
               GROUP BY fc.ConversationUid
          ) pending
          OUTER APPLY
          (
              SELECT TOP 1 fc2.Subject
                FROM dbo.MailMessageReference mr2
               JOIN dbo.MailMessageFullContent fc2
                  ON fc2.Id = mr2.FullContentId
               WHERE mr2.FullContentId IS NOT NULL
                 AND fc2.ConversationUid COLLATE DATABASE_DEFAULT = pending.ConversationUid COLLATE DATABASE_DEFAULT
               ORDER BY mr2.InternalDt DESC, mr2.Id DESC
          ) latest;

        UPDATE mc
           SET mc.Hint = s.Hint,
               mc.ConversationEndDt = s.ConversationEndDt
          FROM dbo.MailConversation mc
          JOIN #ConversationSeeds s
            ON mc.ConversationUid COLLATE DATABASE_DEFAULT = s.ConversationUid COLLATE DATABASE_DEFAULT;

        INSERT INTO dbo.MailConversation (ConversationUid, Hint, ConversationEndDt)
        SELECT s.ConversationUid, s.Hint, s.ConversationEndDt
          FROM #ConversationSeeds s
          LEFT JOIN dbo.MailConversation mc
            ON mc.ConversationUid COLLATE DATABASE_DEFAULT = s.ConversationUid COLLATE DATABASE_DEFAULT
         WHERE mc.Id IS NULL;

        SET @CreatedCount = @@ROWCOUNT;

        UPDATE mr
           SET mr.ConversationId = mc.Id
          FROM dbo.MailMessageReference mr
          JOIN dbo.MailMessageFullContent fc
            ON fc.Id = mr.FullContentId
          JOIN dbo.MailConversation mc
            ON mc.ConversationUid COLLATE DATABASE_DEFAULT = fc.ConversationUid COLLATE DATABASE_DEFAULT
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
