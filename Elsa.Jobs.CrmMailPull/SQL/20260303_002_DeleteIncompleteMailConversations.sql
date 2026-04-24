CREATE OR ALTER PROCEDURE DeleteIncompleteMailConversations
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @DeletedCount int = 0;

    BEGIN TRY
        BEGIN TRAN;

        IF OBJECT_ID('tempdb..#ConversationsToDelete') IS NOT NULL
            DROP TABLE #ConversationsToDelete;

        CREATE TABLE #ConversationsToDelete
        (
            ConversationId int NOT NULL PRIMARY KEY,
            SummaryId int NULL
        );

        ;WITH OrphanUids AS
        (
            SELECT DISTINCT fc.ConversationUid
            FROM dbo.MailMessageReference r
            JOIN dbo.MailMessageFullContent fc
                ON fc.Id = r.FullContentId
            WHERE r.ConversationId IS NULL
              AND r.FullContentId IS NOT NULL
              AND fc.ConversationUid IS NOT NULL
        ),
        ConversationsToDelete AS
        (
            SELECT DISTINCT r2.ConversationId,
                            mc.SummaryId
            FROM dbo.MailMessageReference r2
            JOIN dbo.MailConversation mc
                ON mc.Id = r2.ConversationId
            JOIN dbo.MailMessageFullContent fc2
                ON fc2.Id = r2.FullContentId
            JOIN OrphanUids ou
                ON ou.ConversationUid = fc2.ConversationUid
            WHERE r2.ConversationId IS NOT NULL
        )
        INSERT INTO #ConversationsToDelete (ConversationId, SummaryId)
        SELECT ConversationId, SummaryId
        FROM ConversationsToDelete;

        UPDATE r
            SET r.ConversationId = NULL
        FROM dbo.MailMessageReference r
        JOIN #ConversationsToDelete d
            ON d.ConversationId = r.ConversationId;

        DELETE mc
        FROM dbo.MailConversation mc
        JOIN #ConversationsToDelete d
            ON d.ConversationId = mc.Id;

        DELETE mcs
        FROM dbo.MailConversationSummary mcs
        JOIN #ConversationsToDelete d
            ON d.SummaryId = mcs.Id;

        SET @DeletedCount = @@ROWCOUNT;

        COMMIT;

        SELECT @DeletedCount AS DeletedConversations;
        RETURN @DeletedCount;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO
