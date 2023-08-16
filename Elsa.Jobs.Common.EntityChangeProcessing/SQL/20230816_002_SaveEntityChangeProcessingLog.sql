IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'UDT_EntityChangeProcessingLog' AND is_table_type = 1)
BEGIN
    -- Create the user-defined table type
    CREATE TYPE UDT_EntityChangeProcessingLog AS TABLE
    (
        [EntityId] [bigint],
        [EntityHash] [nvarchar](300),
        [ExternalId] [nvarchar](300),
        [CustomData] [nvarchar](max) NULL
    );
    PRINT 'UDT_EntityChangeProcessingLog created successfully.'
END

GO

IF OBJECT_ID('dbo.SaveEntityChangeProcessingLog', 'P') IS NOT NULL
    DROP PROCEDURE dbo.SaveEntityChangeProcessingLog

GO

CREATE PROCEDURE dbo.SaveEntityChangeProcessingLog
(
	@processorName NVARCHAR(300),
    @ChangeLog UDT_EntityChangeProcessingLog READONLY
)
AS
BEGIN
    MERGE INTO EntityChangeProcessingLog AS target
    USING @ChangeLog AS source
    ON target.ProcessorName = @processorName
       AND target.EntityId = source.EntityId
    WHEN MATCHED THEN
        UPDATE SET
            target.EntityHash = source.EntityHash,
            target.ExternalId = source.ExternalId,
            target.ProcessedDt = GETDATE(),
            target.CustomData = source.CustomData
    WHEN NOT MATCHED THEN
        INSERT (ProcessorName, EntityId, EntityHash, ExternalId, ProcessedDt, CustomData)
        VALUES (@ProcessorName, source.EntityId, source.EntityHash, source.ExternalId, GETDATE(), source.CustomData);
END