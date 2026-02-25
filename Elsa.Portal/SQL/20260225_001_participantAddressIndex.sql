
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = 'IX_MessageParticipantAddress_Email'
      AND i.object_id = OBJECT_ID('dbo.MessageParticipantAddress')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MessageParticipantAddress_Email]
    ON [dbo].[MessageParticipantAddress] ([Email]);
END
GO