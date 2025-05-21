IF NOT EXISTS (
    SELECT * 
    FROM sys.tables 
    WHERE name = 'PureBatchPriceCalculation' 
)
BEGIN
    CREATE TABLE dbo.PureBatchPriceCalculation (
        BatchId INT,
        Amount DECIMAL(19,4),
        Price DECIMAL(19,4),
        PricePerUnit DECIMAL(19,4),
        CalcDt DATETIME
    );

    CREATE INDEX IX_PureBatchPriceCalculation_BatchId
    ON dbo.PureBatchPriceCalculation (BatchId);
END;

GO

IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'OnBatchChanged')
	DROP PROCEDURE OnBatchChanged;

GO

CREATE PROCEDURE OnBatchChanged
(
	@batchId INT
)
AS
BEGIN
  DELETE FROM BatchPriceComponent WHERE BatchId IN (SELECT BatchId FROM dbo.GetAllBatchesContainingProvidedBatch(@batchID)); 
  DELETE FROM PureBatchPriceCalculation WHERE BatchId IN (SELECT BatchId FROM dbo.GetAllBatchesContainingProvidedBatch(@batchID)); 
END

GO