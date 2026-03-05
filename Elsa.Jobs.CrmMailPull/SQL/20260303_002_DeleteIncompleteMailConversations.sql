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
            ConversationId int NOT NULL PRIMARY KEY
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
            SELECT DISTINCT r2.ConversationId
            FROM dbo.MailMessageReference r2
            JOIN dbo.MailMessageFullContent fc2
                ON fc2.Id = r2.FullContentId
            JOIN OrphanUids ou
                ON ou.ConversationUid = fc2.ConversationUid
            WHERE r2.ConversationId IS NOT NULL
        )
        INSERT INTO #ConversationsToDelete (ConversationId)
        SELECT ConversationId
        FROM ConversationsToDelete;

        -- 1) Odpoj reference na konverzaci
        UPDATE r
            SET r.ConversationId = NULL
        FROM dbo.MailMessageReference r
        JOIN #ConversationsToDelete d
            ON d.ConversationId = r.ConversationId;

        -- 2) Smaž konverzace
        DELETE mc
        FROM dbo.MailConversation mc
        JOIN #ConversationsToDelete d
            ON d.ConversationId = mc.Id;

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