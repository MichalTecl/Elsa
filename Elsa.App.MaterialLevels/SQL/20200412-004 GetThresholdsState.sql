IF EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE Name = 'GetThresholdsState')
BEGIN
	DROP PROCEDURE GetThresholdsState;
END

GO

CREATE PROCEDURE GetThresholdsState(@projectId INT, @userId INT)
AS
BEGIN
    DECLARE @avail TABLE (BatchId INT, UnitId INT, Available DECIMAL(19,4));
	
	DECLARE @res TABLE (InventoryId INT NOT NULL,
						MaterialId  INT NOT NULL,
						MaterialName NVARCHAR(256) NOT NULL,
						ThresholdQuantity DECIMAL(19,4) NOT NULL,
						UnitId   INT NOT NULL,
						Available  DECIMAL(19, 4) NULL);

	INSERT INTO @res (InventoryId, MaterialId, MaterialName, ThresholdQuantity, UnitId)
	SELECT  m.InventoryId, m.Id, m.Name, th.ThresholdQuantity, th.UnitId
	  FROM MaterialThreshold th
	  JOIN Material          m  ON (th.MaterialId = m.Id)
	  JOIN UserWatchedInventory uwi ON (uwi.InventoryId = m.InventoryId)
	 WHERE th.ProjectId = @projectId
	   AND m.ProjectId = @projectId;

  WHILE EXISTS(SELECT TOP 1 1 FROM @res WHERE Available IS NULL)
  BEGIN
	DECLARE @matid INT;
	DECLARE @unit  INT;
	SELECT TOP 1 @matid = MaterialId, @unit = UnitId FROM @res WHERE Available IS NULL;

	DELETE FROM @avail;
	INSERT INTO @avail
	EXEC CalculateBatchUsages @ProjectId = @projectId, @MaterialId = @matid;

	UPDATE @res
	  SET Available = ISNULL((SELECT SUM(dbo.ConvertToUnit(@projectId, a.Available, a.UnitId, @unit)) 
	                     FROM @avail a), 0)
    WHERE MaterialId = @matid;
  END

  SELECT * FROM @res WHERE ThresholdQuantity > Available;

END

