IF EXISTS(SELECT TOP 1 1 FROM sys.procedures WHERE name = 'sp_assignEshopItemToMaterial')
	DROP PROCEDURE [sp_assignEshopItemToMaterial];

GO

CREATE PROCEDURE [sp_assignEshopItemToMaterial] (@placedName NVARCHAR(400), @materialName NVARCHAR(200), @tagName NVARCHAR(100), @projectId INT)
AS
BEGIN	

	DECLARE @materialId INT = (SELECT TOP 1 Id FROM Material WHERE Name = @materialName AND ProjectId = @projectId);
	
	if (@materialId IS NULL)
	BEGIN
		SELECT N'Neexistující materiál "' + @materialName + '"';
		RETURN;
	END

	DECLARE @vmId INT = (SELECT TOP 1 Id FROM VirtualProduct WHERE Name = @tagName AND ProjectId = @projectId);

	IF (@vmId IS NULL)
	BEGIN
		INSERT INTO VirtualProduct (ProjectId, Name) VALUES (@projectId, @tagName);
		SET @vmId = SCOPE_IDENTITY();
	END

	DELETE FROM VirtualProductMaterial WHERE VirtualProductId = @vmId OR ComponentId = @materialId;
		  
	INSERT INTO VirtualProductMaterial (VirtualProductId, ComponentId, UnitId, Amount)
	VALUES (@vmId, @materialId, 3, 1);

	DELETE FROM VirtualProductOrderItemMapping WHERE ProjectId = @projectId AND ItemName = @placedName;
	
	INSERT INTO VirtualProductOrderItemMapping (ProjectId, ItemName, VirtualProductId)
	VALUES (@projectId, @placedName, @vmId);
END
GO